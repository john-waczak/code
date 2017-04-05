/*

John Waczak
CES 
09/06/2016

This program contains classes for dealing with modbus, SQLite, and the Gill wind sensor

The main() method contains examples for writing registers, gettting wind data, and 
sending it all to the adapt db 

If you have any questions email me at: waczakj@oregonstate.edu
or call/text 503.330.1280 

*/




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// for SQLite
using System.Data.SQLite;

// for modbus 
using System.Threading;
using Modbus;
using Modbus.Data;
using Modbus.Device;
using Modbus.Utility;

// for comm ports 
using System.IO.Ports;


// for log file writing 
using System.IO;



namespace XactDataHandler
{
    class Program
    {
        static void Main(string[] args)
        {

            /*          get wind data           */

            string windComm = "COM5";
            gillWindSonicSensor windSensor = new gillWindSonicSensor();
            SerialPort port = windSensor.initialize(windComm);


            Console.WriteLine("Enter sample minutes");
            int sampleMinutes = int.Parse(Console.ReadLine());
            int sampleTime =30 * sampleMinutes; 


            gillWindSensorUtilities util = new gillWindSensorUtilities(); 
            string[] dirArray = new string[sampleTime];
            string[] speedArray = new string[sampleTime];
            string windUnits = ""; 
            for (int i = 0; i < sampleTime; i++)
            {
                windSensor.sendDataRequest(port);
                Thread.Sleep(2000);
                string data = windSensor.getData(port); 
                if(windSensor.checkDataLength(data) == true)
                {
                    Console.WriteLine("\n" + data + "\n"); 
                    dirArray[i] = windSensor.getWindDirection(data);
                    speedArray[i] = windSensor.getWindSpeed(data); 
                }


                windUnits = windSensor.getWindUnits(data); // for when you send to adapt db
               
                
            }
            float[] averagedWindData = util.averagedWindData(speedArray, dirArray);
            Console.WriteLine("Average wind speed:\t{0}", averagedWindData[0]);
            Console.WriteLine("Average wind dir:\t{0}", averagedWindData[1]);



            /*           getting the data from each file, formatting, and sending to registers/db */

            nModbus modbus = new nModbus();
            DataStore xactHoldingRegisters;
            modbus.initialize("COM3", 9200, out xactHoldingRegisters);


            int beginAddress = 100;

            string[] fileNames = Directory.GetFiles("U:\\JohnW\\Dev\\process data");
            foreach (var file in fileNames)
            {
                /*          get conc data           */
           
                string[] processData = modbus.getConcDataAndAquisitionTime(file);
                Console.WriteLine("\nData aquired from Process Data file: {0}\n", file);

                ushort[] modbusDataArray = modbus.createModbusDataArray(processData);
                Console.WriteLine("Data now formatted");

                modbus.updateModbusSlaveRegisters(xactHoldingRegisters, modbusDataArray, beginAddress);




                /*          write to adapt db           */
                AdaptDBsqlite db = new AdaptDBsqlite();
                SQLiteConnection dbConnection;
                db.createDBconnection(@"U:\JohnW\Dev\SQLite\SQLite databases and application\v_1_4_0\adapt.db", out dbConnection);

                db.updateDateTime(dbConnection, processData);
                db.updateXactData(processData, dbConnection);
                db.updateWindData(dbConnection, processData[0], averagedWindData[0], averagedWindData[1], windUnits);
                // make sure to add an updateInstrumentStatusData method
                Console.WriteLine("\nAll data sent");


                beginAddress += 200; 
            } 


            
            Console.ReadKey(); 
        }
    }


    /// <summary>
    /// This class is for dealing with Modbus
    ///     * creating slave device
    ///     * creating registers
    ///     * formatting concentration data
    ///     * writing to registers 
    /// </summary>
    class nModbus
    {




        /// <summary>
        /// This method creates a modbus slave on the desired port.
        /// </summary>
        /// <param name="commPort">
        /// The name for the slave serial port
        /// </param>
        /// <param name="baudRate">
        /// the serial port baud rate
        /// </param>
        /// <param name="dataStore">
        /// The data store object used for registers
        /// </param>
        public void initialize(string commPort, int baudRate, out DataStore dataStore)
        {
            //configure Modbus Slave 
            SerialPort slavePort = new SerialPort(commPort);
            slavePort.BaudRate = baudRate;
            slavePort.DataBits = 8;
            slavePort.Parity = Parity.None;
            slavePort.StopBits = StopBits.One;
            slavePort.Open();

            byte slaveId = 1;

            //create slave on seperate thread
            ModbusSlave slave = ModbusSerialSlave.CreateRtu(slaveId, slavePort);
            slave.DataStore = DataStoreFactory.CreateDefaultDataStore();
            dataStore = slave.DataStore;
            Thread slaveThread = new Thread(new ThreadStart(slave.Listen));
            slaveThread.Start();
        }









        /// <summary>
        /// This method looks into the Process Data txt file and parses out the date time, 
        /// concentrations, and uncertainties
        /// </summary>
        /// <param name="filename">
        /// path to the process data text file 
        /// </param>
        /// <returns>
        /// Returns a string array with the desired data
        /// </returns>
        public string[] getConcDataAndAquisitionTime(string filename)
        {
            //get the lines from the csv data file 
            string[] rawXactData = System.IO.File.ReadAllLines(filename);

            string[] dataToReturn = new string[(rawXactData.Length - 18) * 4 + 1]; //numbers from process data txt file 

            //get datetime format for ADAPT
            string[] line1split = rawXactData[0].Split(',');
            string[] dateTime = line1split[1].Split('-', ' ', ':', '/');

            dataToReturn[0] = dateTime[2] + dateTime[0] + dateTime[1] + dateTime[3] + dateTime[4] + dateTime[5];

            int dataToReturnIndex = 1;
            //get atomic number, concentration, and error 
            for (int i = 18; i < rawXactData.Length; i++)
            {
                string[] splitLine = rawXactData[i].Split(',');

                dataToReturn[dataToReturnIndex] = splitLine[0];
                dataToReturn[dataToReturnIndex + 1] = splitLine[1];
                dataToReturn[dataToReturnIndex + 2] = splitLine[2];
                dataToReturn[dataToReturnIndex + 3] = splitLine[3];

                dataToReturnIndex += 4;

            }

            return dataToReturn;
        }










        /// <summary>
        /// This method takes a floating point number and converts it into IEEE 754 standard integer representation
        /// </summary>
        /// <param name="numberToWrite">
        /// The number you would like to store in two registers
        /// </param>
        /// <returns>
        /// Returns a ushort array containing the integers for the modbus registers
        /// </returns>
        public ushort[] floatToIntegersForRegisters(float numberToWrite)
        {
            // get the IEE 754 bit representation of Float 
            var ieeeRepresentation = BitConverter.GetBytes(numberToWrite);

            // Split the number int two 16 bit chunks and set them to integers

            int low = ieeeRepresentation[0] | (ieeeRepresentation[1] << 8);
            int high = ieeeRepresentation[2] | (ieeeRepresentation[3] << 8);


            // convert the integers to ushort to make Modbus happy later

            ushort[] dataToWrite = new ushort[] { (ushort)low, (ushort)high };
            return dataToWrite;
        }






        /// <summary>
        /// This method creates an array from the retrieved data that to be stored in 
        /// modbus registers 
        /// </summary>
        /// <param name="data">
        /// The string array of data retrieved with getConcDataAndAquisitionTime()
        /// </param>
        /// <returns>
        /// Returns a ushort array to be passed to the modbus registers
        /// </returns>
        public ushort[] createModbusDataArray(string[] data)
        {


            int numberOfAnalytes = 0;

            for (int i = 1; i < data.Length; i += 4)
            {
                if (data[i] != "Xx")
                {
                    numberOfAnalytes += 1;
                }
            }

            int dataLength = (numberOfAnalytes * 4) + 3; // 3 is for the three date time ints and there are 2 floats for conc and uncert that both have 2 ints. 


            //create the ushort array for the modbus slave 
            ushort[] modbusDataArray = new ushort[dataLength];
            int modbusDataArrayIndex = 3;


            // add date time to dataToBeWritten 

            char[] dateTimeChars = data[0].ToCharArray();

            //year//
            char[] yearChars = new char[4] { dateTimeChars[0], dateTimeChars[1], dateTimeChars[2], dateTimeChars[3] };
            string year = new string(yearChars);
            int yearForModbus = int.Parse(year);

            //month-day//
            char[] monthDayChars = new char[4] { dateTimeChars[4], dateTimeChars[5], dateTimeChars[6], dateTimeChars[7] };
            string monthDay = new string(monthDayChars);
            int monthDayForModbus = int.Parse(monthDay);

            char[] hoursMinutesChars = new char[4] { dateTimeChars[8], dateTimeChars[9], dateTimeChars[10], dateTimeChars[11] };
            string hoursMinutes = new string(hoursMinutesChars);
            int hoursMinutesForModbus = int.Parse(hoursMinutes);

            modbusDataArray[0] = (ushort)yearForModbus;
            modbusDataArray[1] = (ushort)monthDayForModbus;
            modbusDataArray[2] = (ushort)hoursMinutesForModbus;




            for (int i = 2; i < data.Length; i += 4)
            {

                //make sure we aren't writing data for an interfering analyte 
                if (data[i - 1] != "Xx")
                {
                    ushort atomicNumber = ushort.Parse(data[i]);
                    ushort[] conc = floatToIntegersForRegisters(float.Parse(data[i + 1]));
                    ushort[] uncertainty = floatToIntegersForRegisters(float.Parse(data[i + 2]));


                    modbusDataArray[modbusDataArrayIndex] = conc[0];
                    modbusDataArray[modbusDataArrayIndex + 1] = conc[1];
                    modbusDataArray[modbusDataArrayIndex + 2] = uncertainty[0];
                    modbusDataArray[modbusDataArrayIndex + 3] = uncertainty[1];

                    modbusDataArrayIndex += 4;
                }
            }

            return modbusDataArray;
        }






        /// <summary>
        /// This method takes a ushort array of data and writes it to consecutive modbus registers
        /// </summary>
        /// <param name="dataStore">
        /// The dataStore object used by the modbus registers
        /// </param>
        /// <param name="dataToWrite">
        /// the ushort array of data you want to write
        /// </param>
        /// <param name="startAddress">
        /// The first modbus register address you would like to write to
        /// </param>
        public void updateModbusSlaveRegisters(DataStore dataStore, ushort[] dataToWrite, int startAddress)
        {


            for (int i = 0; i < dataToWrite.Length; i++) //we want to start at address = 1 to make modbus happy but need to get all the values written 
            {

                dataStore.HoldingRegisters[startAddress] = dataToWrite[i];
                startAddress++;
            }

        }


        
       
    }







    /// <summary>
    /// This class is for writing data to the adapt database 
    /// The key tables that need to be updated are: 
    ///     * dateTime
    ///     * xactData
    ///     * instrumentStatusData
    ///     * windData
    /// </summary>
    class AdaptDBsqlite
    {


        /// <summary>
        /// Creates a connection to the adapt database 
        /// </summary>
        /// <param name="dbPath">
        /// The path for the adapt database 
        /// </param>
        public void createDBconnection(string dbPath, out SQLiteConnection dbConnection)
        {
            
            dbConnection = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;");
            dbConnection.Open();

        }








        /// <summary>
        /// This method updates the dateTime table on the adapt db 
        /// </summary>
        /// <param name="dbConnection">
        /// the db connection object for the adapt db 
        /// </param>
        /// <param name="data">
        /// the string array you are taking the data from (originally from process data) 
        /// </param>
        public void updateDateTime(SQLiteConnection dbConnection, string[] data)
        {
            string checkDateTimeTable = "select date_time from dateTime";
            SQLiteCommand dateTimeRedundancy = new SQLiteCommand(checkDateTimeTable, dbConnection);
            SQLiteDataReader reader = dateTimeRedundancy.ExecuteReader();

            List<string> dateTimeList = new List<string>();

            while (reader.Read())
            {
                dateTimeList.Add(reader.GetInt64(0).ToString());
            }
            reader.Close();

            //CHECKING to make sure date time isn't already in database 

            if (dateTimeList.Contains(data[0]))
            {
                Console.WriteLine("\n Error: dateTime already exists in db. \n\t You may be duplicating data.");

            }
            else
            {
                string sendDateTime = String.Format("insert into dateTime(date_time) values({0})", data[0]);
                SQLiteCommand insertDateTime = new SQLiteCommand(sendDateTime, dbConnection);
                insertDateTime.ExecuteNonQuery();

                Console.WriteLine("\nDateTime sent to database...\n");
            }
        }





        /// <summary>
        /// This method updates the xactData table on the adapt database
        /// </summary>
        /// <param name="data">
        /// A string array from the process data text file 
        /// </param>
        /// <param name="dbConnection">
        /// The connection object created with createDBconnection() 
        /// </param>
        public void updateXactData(string[] data, SQLiteConnection dbConnection)
        {
            string[] valuesForDatabase = new string[(data.Length - 1) / 4];

            int valuesIndex = 0;
            string concentration;
            string concentration_err;
            string analyte_pk;
            string concUnits_pk = "1"; // ng/m^3 
            string mdl_exp;

            /* --------------May need to change----------------*/
            string sampleTime_pk = "1"; //this might need to be changed ... see database 
            /*-------------------------------------------------*/


            for (int i = 2; i < data.Length; i += 4)
            {

                analyte_pk = data[i];
                concentration = data[i + 1];
                concentration_err = data[i + 2];

                if (data[i - 1] != "Xx")
                {
                    //querry database for mdl_exp
                    string getMDLstring = String.Format("SELECT mdl_exp FROM mdl WHERE sampleTime_pk = {0} AND analyte_pk = {1};", sampleTime_pk, analyte_pk);
                    SQLiteCommand getMDL = new SQLiteCommand(getMDLstring, dbConnection);
                    mdl_exp = Convert.ToString(getMDL.ExecuteScalar());


                    if (mdl_exp.Length > 0)
                    {
                        valuesForDatabase[valuesIndex] = String.Format("insert into xactData(concentration, concentration_err, mdl_exp, dateTime_pk, analyte_pk, concUnits_pk)  values ({0}, {1}, {2}, {3}, {4}, {5})",
                       concentration, concentration_err, mdl_exp, data[0], analyte_pk, concUnits_pk);
                    }
                    else
                    {
                        valuesForDatabase[valuesIndex] = String.Format("insert into xactData(concentration, concentration_err, dateTime_pk, analyte_pk, concUnits_pk)  values ({0}, {1}, {2}, {3}, {4} )",
                       concentration, concentration_err, data[0], analyte_pk, concUnits_pk);
                    } // it seems like if the MDL is missing, adapt wont show ANY of the data 


                    valuesIndex += 1;
                }
            }
            // use the commands  
            for (int i = 0; i < valuesForDatabase.Length; i++)
            {
                if (valuesForDatabase[i] != null)
                {

                    SQLiteCommand insertIntoTable = new SQLiteCommand(valuesForDatabase[i], dbConnection);
                    insertIntoTable.ExecuteNonQuery();
                }

            }
            Console.WriteLine("\nData sent to xactData table.\n");
        }





        /// <summary>
        /// Closes the connection to the adapt db
        /// </summary>
        /// <param name="dbConnection">
        /// the db connection object for adapt db 
        /// </param>
        public void closeDBconnection(SQLiteConnection dbConnection)
        {
            dbConnection.Close(); 
        }








        /// <summary>
        /// This method is for updating the instrumentStatusData table on the adapt db
        /// </summary>
        /// <param name="dbConnection"></param>
        public void updateInstrumentStatusData(SQLiteConnection dbConnection)
        {
            /*
            Need to add code 
            */ 
        }






        /// <summary>
        /// This method is for updating the windData table on the adapt db
        /// </summary>
        /// <param name="dbConnection"></param>
        public void updateWindData(SQLiteConnection dbConnection, string dateTime, float speed, float direction, string units)
        {
            
            string windSpeed = Convert.ToString(speed);
            string windDir = Convert.ToString(direction);
            int windDirUnits_pk = 1;
            int windSpeedUnits_pk; 
            

            // adam also lists '@' and 'P' as units by mph and m/s in the windSpeedUnits Table
            if(units == "P")
            {
                windSpeedUnits_pk = 3; 
            }
            else if (units == "M")
            {
                windSpeedUnits_pk = 4; 
            }
            else
            {
                windSpeedUnits_pk = 1; // just here to make the command string happy 
            }

            // create command string 
            string sendWindData = String.Format("insert into windData (wind_dir, wind_speed, windDirUnits_pk, windSpeedUnits_pk, dateTime_pk) values({0}, {1}, {2}, {3}, {4})", windDir, windSpeed, windDirUnits_pk, windSpeedUnits_pk, dateTime);
            SQLiteCommand insertIntoWindDataTable = new SQLiteCommand(sendWindData, dbConnection);

            //send command
            insertIntoWindDataTable.ExecuteNonQuery();

            Console.WriteLine("\nData sent to windData table");
        }
             
    }








    /// <summary>
    /// This class if for querrying the Gill WindSonic sensor 
    /// </summary>
    class gillWindSonicSensor
    {
        /// <summary>
        /// Initializes a serial port for the Gill Wind Sensor
        /// </summary>
        /// <param name="portName">
        /// COM1, COM2, COM3,etc...
        /// </param>
        /// <param name="port">
        /// Name of the SerialPort object
        /// </param>
        public SerialPort initialize(string portName)
        {
            SerialPort port = new SerialPort();
            port.PortName = portName;
            port.BaudRate = 19200;
            port.Parity = Parity.None;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Open();
            return port;
        }



        /// <summary>
        ///  Change the baud rate
        /// </summary>
        /// <param name="port">
        /// name of SerialPort object
        /// </param>
        /// <param name="newBaudRate">
        /// the new baud rate (int) 
        /// </param>
        public void changeBaudRate(SerialPort port, int newBaudRate)
        {
            port.BaudRate = newBaudRate;
        }



        /// <summary>
        /// Changes which comm port the wind sensor is connected to
        /// </summary>
        /// <param name="port">
        /// name of the SerialPort object
        /// </param>
        /// <param name="portName">
        /// the new port you would like for the connection 
        /// </param>
        public void changePort(SerialPort port, string portName)
        {
            port.PortName = portName;
        }






        /// <summary>
        /// Closes the Serial Port
        /// </summary>
        /// <param name="port">
        /// name of the SerialPort object
        /// </param>
        public void closePort(SerialPort port)
        {
            port.Close();
        }



        /// <summary>
        /// Looks for data on the serial port from the Gill windsensor
        /// </summary>
        /// <param name="port">
        /// The name of the SerialPort object
        /// </param>
        /// <returns>
        /// A string called data is returned containing all of the serial
        /// information querried from the sensor. 
        /// </returns>
        public string getData(SerialPort port)
        {
            byte[] byteArray = new byte[port.BytesToRead];
            string dataString = " ";

            if (port.BytesToRead > 0)
            {
                try
                {
                    port.Read(byteArray, 0, byteArray.Length);
                    dataString = Encoding.ASCII.GetString(byteArray);

                }
                catch (TimeoutException)
                {
                    Console.WriteLine("TimeoutException: Failed to talk to sensor");
                    dataString = "no data";
                }
            }
            else
            {
                dataString = "no data";
            }


            return dataString;
        }


        /// <summary>
        /// Querry the Gill wind sensor for data. 
        /// </summary>
        /// <param name="port">
        /// The name of the SerialPort object. 
        /// </param>
        public void sendDataRequest(SerialPort port)
        {
            port.WriteLine("?"); // ensures you are in polled mode
            port.WriteLine("T"); // this is the unit identifier... this can be changed with the gill software 
        }







        /// <summary>
        /// This method checks the data you've recieved to make sure you are getting a full sample
        /// </summary>
        /// <param name="data">
        /// The string that contains the Gill winds sensor data. See: getData()
        /// </param>
        /// <returns>
        /// Returns true if data string is full. 
        /// </returns>
        public bool checkDataLength(string data)
        {
            bool trueFalse;
            if (data.Length > 0)
            {
                trueFalse = true;
            }
            else
            {
                trueFalse = false;
            }

            return trueFalse;
        }


        /// <summary>
        /// This method parses the Gill wind sensor data string and returns the wind direction 
        /// </summary>
        /// <param name="data">
        /// The Gill wind sensor data string
        /// </param>
        /// <returns>
        /// wind Direction string (0 to 359 I believe) in degrees 
        /// </returns>
        public string getWindDirection(string data)
        {
            string[] splitData = data.Split(',');
            string windDir;
            
            if (splitData[1] != null)
            {
                windDir = splitData[1];
            }

            else
            {
                windDir = "0.00";
            }

            return windDir;
        }



        /// <summary>
        /// This method parses the Gill wind sensor data string and returns the wind speed 
        /// </summary>
        /// <param name="data">
        /// The Gill wind sensor data string
        /// </param>
        /// <returns>
        /// wind speed string 
        /// </returns>
        public string getWindSpeed(string data)
        {
            string[] splitData = data.Split(',');
            string windSpeed;     

            if (splitData[2] != null)
            {
                windSpeed = splitData[2];
            }

            else
            {
                windSpeed = "0.00";
            }

            return windSpeed;
        }

        /// <summary>
        /// This method parses the Gill wind sensor data string and returns the Gill code for the units (i.e. 'P' for mph, 'M' for m/s, etc...) 
        /// </summary>
        /// <param name="data">
        /// The Gill wind sensor data string
        /// </param>
        /// <returns>
        /// wind units string 
        /// </returns>
        public string getWindUnits(string data)
        {

            string[] splitData = data.Split(',');
            string windUnits;

            if (splitData[3] != null)
            {
                 windUnits = splitData[3];
            }

            else
            {
                windUnits = "0.00"; 
            }
            return windUnits;
        }



        /// <summary>
        ///  This method will log the Gill wind sensor data stream to the specified path
        /// </summary>
        /// <param name="data">
        /// The Gill wind sensor data string
        /// </param>
        /// <param name="path">
        /// The file path for the txt file you would like to write to
        /// </param>
        public void logWindData(string data, string path)
        {
            StreamWriter fileLogger = new StreamWriter(path, true);
            fileLogger.WriteLine(data);
            fileLogger.Close();
        }


    }



    /// <summary>
    /// Utilities for Gill Wind Sensor Data
    /// </summary>
    public class gillWindSensorUtilities
    {


        /// <summary>
        /// Takes a string and tries to return it's corresponding float value
        /// </summary>
        /// <param name="String">
        /// The string you want to convert 
        /// </param>
        /// <returns>
        /// float
        /// </returns>
        public float getFloat(string String)
        {

            float returnFloat; 

            try
            {
                returnFloat = float.Parse(String);
            }
            catch (Exception)
            {

                returnFloat = 0.0f; 
            }

            return returnFloat; 

        }


        /// <summary>
        /// Thakes the wind speed data and direction data and returns
        /// the average speed and direction for a sample period 
        /// </summary>
        /// <param name="speed">
        /// string array holding the wind speed data
        /// </param>
        /// <param name="dir">
        /// string array holding the direction data
        /// </param>
        /// <returns>
        /// returns and array containing two floats. The first is the speed
        /// and the second is the direction. 
        /// </returns>
        public float[] averagedWindData(string[] speed, string[] dir)
        {
            // first convert string[] to float[] 
            float[] speedFloat = new float[speed.Length];
            float[] dirFloat = new float[dir.Length];


            // speed.Length should always equal dir.Length
            for (int i = 0; i < speed.Length; i++)
            {
                speedFloat[i] = getFloat(speed[i]);
                dirFloat[i] = getFloat(dir[i]);
            }

            //convert to cartesian
            float[] x = new float[speedFloat.Length];
            float[] y = new float[speedFloat.Length]; 

            for (int i = 0; i < speedFloat.Length; i++)
            {
                x[i] = (float)((double)speedFloat[i] * (Math.Cos((Math.PI / 180) * (double)dirFloat[i])));
                y[i] = (float)((double)speedFloat[i] * (Math.Sin((Math.PI / 180) * (double)dirFloat[i])));
            }


            // average the x and y components
            float xAverage = x.Average(); 
            float yAverage = y.Average();

            //now use trig to recover speed and direction
            float[] returnData = new float[2];
                //speed
            returnData[0] = (float)(Math.Sqrt(Math.Pow(((double)xAverage), 2) + Math.Pow(((double)yAverage), 2)));
                
            
                //dir      
            returnData[1] = (float)((180 / Math.PI) * (Math.Atan2((double)yAverage, (double)xAverage)));

            if(returnData[1] < 0)
            {
                returnData[1] = 360 + returnData[1]; 
            }
            return returnData; 
        }

    
    }

}
