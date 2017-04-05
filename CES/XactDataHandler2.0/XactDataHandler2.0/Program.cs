using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// for SQLite
using System.Data.SQLite;

// for modbus 
using System.Threading;
using Modbus.Data;
using Modbus.Device;

// for comm ports 
using System.IO.Ports;


// for log file writing 
using System.IO;

namespace XactDataHandler2._0
{
    class Program
    {
        static void Main(string[] args)
        {

            //string path = @"C:\Users\BrianC\Desktop\Process_Data"; // path to the process_data directory                       // instantiating nModbus Class
            string path = @"C:\Process_Data"; // path to the process_data directory                       // instantiating nModbus Class
            string processFile = nModbus.findProcessFile(path);      // finding the most recent log file in process_data

            //AdaptDB db = new AdaptDB(); 
            //SQLiteConnection dbConnection;
            //db.DBConnection(@"\Desktop\v_1_5_0\webapp\adapt.db", out dbConnection);
            AdaptDB db = new AdaptDB();
            SQLiteConnection dbConnection;
            //db.DBConnection("C:\\Users\\BrianC\\Desktop\\V_1_5_1\\adapt.db", out dbConnection);
            //C:/ XactControl / Adapt / adapt.db
            db.DBConnection("C:/XactControl/Adapt/adapt.db", out dbConnection);


            List<string> conc_and_dateTime = new List<string>();                    // creating lists to save raw data
            List<string> instrumentData = new List<string>();
            nModbus.getRawData(processFile, out conc_and_dateTime, out instrumentData);  // get raw data makes a conc list and instrumentData list


            ushort[] modbusDataArray = nModbus.createModbusArray(conc_and_dateTime);     // format the data for modbus (turning floats to ints) 


            ushort[] DateTime = nModbus.modbusDateTimeArray(modbusDataArray);
            ushort[] telemetryData = nModbus.ModbusTelemetryArray(instrumentData);      // here are the five arrays we'll want to write to modbus 
            ushort[] atomicNumbers = nModbus.atomicNumberArray(modbusDataArray);        // registers 
            ushort[] concArray = nModbus.modbusConcArray(modbusDataArray);              
            ushort[] uncArray = nModbus.modbusUncArray(modbusDataArray);


            // update the database 
            db.updateDateTime(dbConnection, conc_and_dateTime, instrumentData);
            db.updateXactData(dbConnection, conc_and_dateTime, instrumentData);
            db.updateInstrumentStatusData(dbConnection, conc_and_dateTime, instrumentData);
            db.updateWindData(dbConnection, conc_and_dateTime, instrumentData);
            dbConnection.Close(); 
            Console.WriteLine("It (probably) Worked!");




            Console.ReadLine(); 




            //// initialize the modbus stuff
            //DataStore xactHoldingRegisters;
            //string modbusPort = "COM1";
            //nModbus.initialize(modbusPort, 2400, out xactHoldingRegisters);

            //int startAddress = 100;

            //Console.WriteLine("Hit Enter to update registers.");
            //Console.ReadLine(); 


            ////update the registers
            //nModbus.updateModbusSlaveRegisters(xactHoldingRegisters, atomicNumbers, startAddress);
            //startAddress += 100; 
            //nModbus.updateModbusSlaveRegisters(xactHoldingRegisters, concArray, startAddress);
            //nModbus.updateModbusSlaveRegisters(xactHoldingRegisters, uncArray, startAddress +concArray.Length);

            //startAddress = 701;
            //nModbus.updateModbusSlaveRegisters(xactHoldingRegisters, DateTime, startAddress);
            //startAddress += 2; 
            //nModbus.updateModbusSlaveRegisters(xactHoldingRegisters, telemetryData, startAddress + DateTime.Length);

            
            //ushort[] mbUpdate = new ushort[1] { 1 };
            //nModbus.updateModbusSlaveRegisters(xactHoldingRegisters, mbUpdate, 700);


            //Console.ReadLine();

        }
    }










    /// <summary>
    /// This class is for dealing with Modbus
    ///     * creating slave device
    ///     * creating registers
    ///     * formatting concentration data
    ///     * writing to registers 
    /// </summary>
    /// 
    //public static partial class Logger
    public static partial class nModbus
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

        public static Thread slaveThread;

        public static void initialize(string commPort, int baudRate, out DataStore dataStore)
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
            slaveThread = new Thread(new ThreadStart(slave.Listen));

            slaveThread.Start();
        }

        /// <summary>
        /// This method looks in the process_data directory and
        /// retrieves the most recent data file 
        /// </summary>
        /// <param name="path">
        /// The path to the process_data directory
        /// </param>
        /// <returns>
        /// A string containing the file's path
        /// </returns>
        public static string findProcessFile(string path)
        {

            System.IO.FileInfo[] sortedFiles = new DirectoryInfo(path).GetFiles().OrderByDescending(f => f.LastWriteTime).ToArray();
            string[] processFiles = new string[sortedFiles.Length];

            for (int i = 0; i < sortedFiles.Length; i++)
            {
                string f = sortedFiles[i].ToString();
                if (!f.Contains("data"))
                {
                    processFiles.SetValue(f, i);
                }
            }
            string targetFile = processFiles.FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? "";
            return path + "\\" + targetFile;
        }

        /// <summary>
        /// This method looks into the Process Data txt file and parses out the date time, 
        /// concentrations, and uncertainties
        /// </summary>
        /// <param name="filename">
        /// path to the process data text file 
        /// </param>
        /// <returns>
        /// Returns a string array with the data
        /// </returns>
        public static void getRawData(string filename, out List<string> conc_and_dateTime, out List<string> instrumentData)
        {

            conc_and_dateTime = new List<string>();
            instrumentData = new List<string>();

            try
            {
                string[] rawData = System.IO.File.ReadAllLines(filename);


                // find Brian's index in process_data file
                var index = Array.IndexOf(rawData, ",*****,");


                //get datetime format for ADAPT 
                string[] line1 = rawData[index+1].Split(','); // index should be changed depending on txt file format
                string[] dateTime = line1[2].Split('-', ' ', ':', '/');
                //conc_and_dateTime.Add(dateTime[2] + dateTime[0] + dateTime[1] + dateTime[3] + dateTime[4] );  //+ dateTime[5]
                conc_and_dateTime.Add(dateTime[2] + dateTime[0] + dateTime[1] + dateTime[3] + "00");  //+ dateTime[5]


                // get the concentrations 
                for (int i = 2; i < index; i++) // index should be changed depending on txt file format
                {
                    string[] splitLine = rawData[i].Split(',');
                    conc_and_dateTime.Add(splitLine[0]); //symbol
                    conc_and_dateTime.Add(splitLine[1]); // atomic number
                    conc_and_dateTime.Add(splitLine[2]); //  mass
                    conc_and_dateTime.Add(splitLine[3]); // uncertainty in mass
                    conc_and_dateTime.Add(splitLine[4]); // concentration
                }

                // get the instrument data
                for (int i = index + 1; i < rawData.Length; i++)
                {
                    string[] splitLine = rawData[i].Split(',');

                    instrumentData.Add(splitLine[2]);
                }
            }
            catch (Exception Ex)
            {


            }

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
        public static ushort[] floatToIntegersForRegisters(float numberToWrite)
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
        public static ushort[] createModbusArray(List<string> data)
        {
            int numberOfAnalytes = (data.Count - 1) / 5;
            int dataLength = numberOfAnalytes * 5 + 3; //3 for datetime, 1 per atomic #, 2 per conc, 2 per unc

            //create the ushort array for the modbus slave 
            ushort[] modbusDataArray = new ushort[dataLength];

            /*--------------------------------------------------------*/

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

            //hours-minutes// 
            char[] hoursMinutesChars = new char[4] { dateTimeChars[8], dateTimeChars[9], dateTimeChars[10], dateTimeChars[11] };
            string hoursMinutes = new string(hoursMinutesChars);
            int hoursMinutesForModbus = int.Parse(hoursMinutes);


            // problem here !!! // 

            modbusDataArray[0] = (ushort)yearForModbus;
            modbusDataArray[1] = (ushort)monthDayForModbus;
            modbusDataArray[2] = (ushort)hoursMinutesForModbus;

            /*--------------------------------------------------------*/

            // get atomic #s, concentrations, and uncertainties

            ushort[] atomicNumbers = new ushort[numberOfAnalytes];
            int counter1 = 0;
            for (int i = 2; i < data.Count; i += 5)
            {
                atomicNumbers[counter1] = ushort.Parse(data[i]);
                counter1 += 1;
            }

            ushort[] concentrations = new ushort[numberOfAnalytes * 2];
            try
            {

                int counter2 = 0;
                for (int i = 5; i < data.Count; i += 5)
                {
                    if (data[i] != "")
                    {
                        ushort[] conc = floatToIntegersForRegisters(float.Parse(data[i]));
                        concentrations[counter2] = conc[0];
                        concentrations[counter2 + 1] = conc[1];
                        counter2 += 2;
                    }
                    else
                    {
                        concentrations[counter2] = 0;
                        concentrations[counter2 + 1] = 0;
                        counter2 += 2;
                    }

                }
            }
            catch (Exception Ex)
            {
                string ex = "Could not get concentration data";
            }


            ushort[] uncertainties = new ushort[numberOfAnalytes * 2];
            try
            {

                int counter3 = 0;
                for (int i = 4; i < data.Count; i += 5)
                {
                    if (data[i] != "")
                    {
                        ushort[] unc = floatToIntegersForRegisters(float.Parse(data[i]));
                        uncertainties[counter3] = unc[0];
                        uncertainties[counter3 + 1] = unc[1];
                        counter3 += 2;
                    }
                    else
                    {
                        uncertainties[counter3] = 0;
                        uncertainties[counter3 + 1] = 0;
                        counter3 += 2;
                    }

                }
            }
            catch (Exception Ex)
            {
                string ex = "Could not get uncertainties data";
            }


            //add atomic numbers, concentrations, and uncertainties to data array
            for (int i = 0; i < atomicNumbers.Length; i++)
            {
                modbusDataArray[i + 3] = atomicNumbers[i];
            }
            for (int i = 0; i < concentrations.Length; i++)
            {
                modbusDataArray[i + 3 + atomicNumbers.Length] = concentrations[i];
            }
            for (int i = 0; i < uncertainties.Length; i++)
            {
                modbusDataArray[i + 3 + atomicNumbers.Length + concentrations.Length] = uncertainties[i];
            }

            return modbusDataArray;
        }

        /// <summary>
        /// Creates Ushort array to send to modbus registers
        /// containing the year, month+day, and hour+min
        /// </summary>
        /// <param name="modbusArray">
        /// Array created with createMobusArray
        /// </param>
        /// <returns></returns>
        public static ushort[] modbusDateTimeArray(ushort[] modbusArray)
        {

            ushort[] returnArray = new ushort[5];

            try
            {

                // set the year
                returnArray[0] = modbusArray[0];

                int sizeMonthDay = modbusArray[1].ToString().Length;
                int sizeHourMin = modbusArray[2].ToString().Length;


                // set the month and day
                if (sizeMonthDay <= 2)
                {
                    returnArray[1] = 0;
                    returnArray[2] = modbusArray[1];
                }
                else if (sizeMonthDay == 3)
                {
                    char[] monthDay = modbusArray[1].ToString().ToCharArray();
                    string month = monthDay[0].ToString();
                    string day = monthDay[1].ToString() + monthDay[2].ToString();
                    returnArray[1] = Convert.ToUInt16(month);
                    returnArray[2] = Convert.ToUInt16(day);
                }
                else if (sizeMonthDay == 4)
                {
                    char[] monthDay = modbusArray[1].ToString().ToCharArray();
                    string month = monthDay[0].ToString() + monthDay[1].ToString();
                    string day = monthDay[2].ToString() + monthDay[3].ToString();
                    returnArray[1] = Convert.ToUInt16(month);
                    returnArray[2] = Convert.ToUInt16(day);
                }
                else
                {
                    returnArray[1] = 0;
                    returnArray[2] = 0;
                }


                // set the hours and minutes 
                if (sizeHourMin <= 2)
                {
                    returnArray[3] = 0;
                    returnArray[4] = modbusArray[2];
                }
                else if (sizeHourMin == 3)
                {
                    char[] hourMin = modbusArray[2].ToString().ToCharArray();
                    string hour = hourMin[0].ToString();
                    string min = hourMin[1].ToString() + hourMin[2].ToString();
                    returnArray[3] = Convert.ToUInt16(hour);
                    returnArray[4] = Convert.ToUInt16(min);
                }
                else if (sizeHourMin == 4)
                {
                    char[] hourMin = modbusArray[2].ToString().ToCharArray();
                    string hour = hourMin[0].ToString() + hourMin[1].ToString();
                    string min = hourMin[2].ToString() + hourMin[3].ToString();
                    returnArray[3] = Convert.ToUInt16(hour);
                    returnArray[4] = Convert.ToUInt16(min);
                }
                else
                {
                    returnArray[3] = 0;
                    returnArray[4] = 0;
                }

                return returnArray;
            }

            catch (Exception ex)
            {
                return returnArray;
            }

        }


        /// <summary>
        /// Creates a ushort array of ints with the atomic 
        /// numbers. 
        /// </summary>
        /// <param name="modbusArray"></param>
        /// <returns></returns>
        /// 
        public static ushort[] atomicNumberArray(ushort[] modbusArray)
        {
            int numberOfAnalytes = (modbusArray.Length - 3) / 5;
            ushort[] atomicNumberArray = new ushort[numberOfAnalytes];
            int counter = 0;

            for (int i = 3; i < numberOfAnalytes + 3; i++)
            {
                atomicNumberArray[counter] = modbusArray[i];
                counter++;

            }

            return atomicNumberArray;
        }


        /// <summary>
        /// Creates a ushort array with concentrations 
        /// </summary>
        /// <param name="modbusArray"></param>
        /// <returns></returns>
        public static ushort[] modbusConcArray(ushort[] modbusArray)
        {
            int numberOfAnalytes = (modbusArray.Length - 3) / 5;
            ushort[] modbusConcArray = new ushort[numberOfAnalytes * 2];
            int counter = 0;

            for (int i = 3 + numberOfAnalytes; i < 3 + numberOfAnalytes * 3; i++)
            {
                modbusConcArray[counter] = modbusArray[i];
                counter++;
            }
            return modbusConcArray;
        }


        /// <summary>
        /// Creates a ushort array with uncertainties 
        /// </summary>
        /// <param name="modbusArray"></param>
        /// <returns></returns>
        public static ushort[] modbusUncArray(ushort[] modbusArray)
        {
            int numberOfAnalytes = (modbusArray.Length - 3) / 5;
            ushort[] modbusUncArray = new ushort[numberOfAnalytes * 2];
            int counter = 0;

            for (int i = 3 + numberOfAnalytes * 3; i < 3 + numberOfAnalytes * 5; i++)
            {
                modbusUncArray[counter] = modbusArray[i];
                counter++;
            }
            return modbusUncArray;
        }


        /// <summary>
        /// Creates a ushort array that can be sent to modbus with the xact instrument data
        /// </summary>
        /// <param name="data">
        /// List containing the telemetry data
        /// </param>
        /// <returns></returns>
        public static ushort[] ModbusTelemetryArray(List<string> data)
        {
            ushort[] returnData = new ushort[(data.Count - 1) * 2];
            int counter = 0;
            for (int i = 2; i < data.Count; i++)
            {
                float parsedFloat = float.Parse(data[i]);
                ushort[] convertedFloat = floatToIntegersForRegisters(parsedFloat);

                returnData[counter] = convertedFloat[0];
                returnData[counter + 1] = convertedFloat[1];
                counter += 2;
            }


            return returnData;
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
        public static void updateModbusSlaveRegisters(DataStore dataStore, ushort[] dataToWrite, int startAddress)
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
    public class AdaptDB
    {


        /// <summary>
        /// Creates a connection to the adapt database 
        /// </summary>
        /// <param name="dbPath">
        /// The path for the adapt database 
        /// </param>
        public void DBConnection(string dbPath, out SQLiteConnection dbConnection)
        {

            dbConnection = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;");
            dbConnection.Open();
        }






        /// <summary>
        /// Updates the dateTime table on the adapt db
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="data"></param>
        public void updateDateTime(SQLiteConnection dbConnection, List<string> data, List<string> instrumentData)
        {

            string dateTime = data[0]; 
            string checkDateTimeTable = String.Format("select date_time from dateTime where date_time like {0}", dateTime);
            SQLiteCommand dateTimeRedundancy = new SQLiteCommand(checkDateTimeTable, dbConnection);
            SQLiteDataReader reader = dateTimeRedundancy.ExecuteReader();


            string sampleTime = instrumentData[17];
            string sampleTime_pk;

            //querry database pk 
            string getPKstring = String.Format("SELECT pk FROM sampleTime where sample_time like {0} ", sampleTime);
            SQLiteCommand getPK = new SQLiteCommand(getPKstring, dbConnection);
            sampleTime_pk = Convert.ToString(getPK.ExecuteScalar());


            List<string> dateTimeList = new List<string>();

            while (reader.Read())
            {
                dateTimeList.Add(reader.GetInt64(0).ToString());
            }
            reader.Close();


            if (dateTimeList.Count <= 0)
            {
                try
                {
                    string sendDateTime = String.Format("insert into dateTime(date_time, sampleTime_pk, sample_completeness) values({0}, {1}, 99)", dateTime, sampleTime_pk);
                    SQLiteCommand insertDateTime = new SQLiteCommand(sendDateTime, dbConnection);
                    insertDateTime.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("dateTime update failed\n");
                }
            }

            else
            {
                Console.WriteLine("dateTime update failed\n");
            }


            

        }


        /// <summary>
        /// updates the xactData table on the adapt db 
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="data"></param>
        public void updateXactData(SQLiteConnection dbConnection, List<string> data, List<string> instrumentData)
        {

            List<string> valuesForDatabase = new List<string>(); 

            string dateTime_pk = data[0];
            string concentration;
            string concentration_err;
            string analyte_pk;
            string concUnits_pk = "1"; // ng/m^3 
            string mdl_exp;


            string sampleTime = instrumentData[17];
            string sampleTime_pk;

            //querry database pk 
            string getPKstring = String.Format("SELECT pk FROM sampleTime where sample_time like {0} ", sampleTime);
            SQLiteCommand getPK = new SQLiteCommand(getPKstring, dbConnection);
            sampleTime_pk = Convert.ToString(getPK.ExecuteScalar());

            //create the db command strings from the data 

            if ((data.Count-1)%5 == 0)
            {
                for (int i = 1; i < data.Count; i += 5)
                {
                    analyte_pk = data[i + 1];
                    concentration_err = data[i + 3];
                    concentration = data[i + 4];


                    //querry database for mdl_exp
                    string getMDLstring = String.Format("SELECT mdl_exp FROM mdl WHERE sampleTime_pk = {0} AND analyte_pk = {1};", sampleTime_pk, analyte_pk);
                    SQLiteCommand getMDL = new SQLiteCommand(getMDLstring, dbConnection);
                    mdl_exp = Convert.ToString(getMDL.ExecuteScalar());

                    if (mdl_exp.Length > 0)
                    {
                       valuesForDatabase.Add(String.Format("insert into xactData(concentration, concentration_err, mdl_exp, dateTime_pk, analyte_pk, concUnits_pk)  values ({0}, {1}, {2}, {3}, {4}, {5})",
                       concentration, concentration_err, mdl_exp, dateTime_pk, analyte_pk, concUnits_pk));
                    }
                    else
                    {
                       valuesForDatabase.Add(String.Format("insert into xactData(concentration, concentration_err, mdl_exp, dateTime_pk, analyte_pk, concUnits_pk)  values ({0}, {1}, {2}, {3}, {4}, {5})",
                       concentration, concentration_err, "1.0", dateTime_pk, analyte_pk, concUnits_pk));
                    } 

                }

                for (int i = 0; i < valuesForDatabase.Count; i++)
                {

                    SQLiteCommand insertIntoTable = new SQLiteCommand(valuesForDatabase[i], dbConnection);
                    insertIntoTable.ExecuteNonQuery();
                }
            }

            else
            {
                Console.WriteLine("The length is incorrect");
            }


        }




        /// <summary>
        /// update the instrumentStatusData table on adapt db
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="data"></param>
        public void updateInstrumentStatusData(SQLiteConnection dbConnection, List<string> conc_and_dateTime, List<string> instrumentData)
        {
             

            //set up column names

            string atm_temp = instrumentData[1];
            string atm_press = instrumentData[3];
            string tape_press = instrumentData[4];
            string samp_temp = instrumentData[2];
            string samp_rh = instrumentData[14]; // to be fixed later 
            string flow_act = instrumentData[6];
            string volume = instrumentData[8];
            string tube_temp = instrumentData[9];
            string enc_temp = instrumentData[10];
            string dateTime_pk = conc_and_dateTime[0];
            string flow_act_qa = "0.0"; 

            //create the command string
            string valuesForDatabase = String.Format("insert into instrumentStatusData(atm_temp, atm_press, tape_press, samp_temp, flow_act, volume, tube_temp, enc_temp, dateTime_pk, flow_act_qa, samp_rh) values ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10})", atm_temp, atm_press, tape_press, samp_temp, flow_act, volume, tube_temp, enc_temp, dateTime_pk, flow_act_qa, samp_rh);
            SQLiteCommand insertIntoTable = new SQLiteCommand(valuesForDatabase, dbConnection);

            try
            {
                insertIntoTable.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Couldn't update instrumentStatusData"); 
            }


        }




        /// <summary>
        /// updates the windData table on the adapt db
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="conc_and_dateTime"></param>
        /// <param name="instrumentData"></param>
        public void updateWindData(SQLiteConnection dbConnection, List<string> conc_and_dateTime, List<string> instrumentData)
        {
            string wind_speed = instrumentData[15];
            string wind_dir = instrumentData[16];
            string windDirUnits_pk = "1";
            string windSpeedUnits_pk = "3";  // may need to add some logic to check the setting 
            string dateTime_pk = conc_and_dateTime[0];

            //create the command string 
            string valuesForDatabase = String.Format("insert into windData (wind_speed, wind_dir, windDirUnits_pk, windSpeedUnits_pk, dateTime_pk) values ({0}, {1}, {2}, {3}, {4})", wind_speed, wind_dir, windDirUnits_pk, windSpeedUnits_pk, dateTime_pk);
            SQLiteCommand insertIntoTable = new SQLiteCommand(valuesForDatabase, dbConnection);

            try
            {
                insertIntoTable.ExecuteNonQuery(); 
            }

            catch (Exception ex)
            {
                Console.WriteLine("Couldn't update windData"); 
            }
        }



    }

    class gillWindSensor
    {

    }

    class gillWindSensorUtilities
    {

    }

}
