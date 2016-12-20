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
            
            string path = @"\\SERVER\Users\JohnW\Process_Data"; // path to the process_data directory
            
            nModbus mb = new nModbus();                         // instantiating nModbus Class
            string processFile = mb.findProcessFile(path);      // finding the most recent log file in process_data



            List<string> conc_and_dateTime = new List<string>();                    // creating lists to save raw data
            List<string> instrumentData = new List<string>();
            mb.getRawData(processFile, out conc_and_dateTime, out instrumentData);  // get raw data makes a conc list and instrumentData list


            ushort[] modbusDataArray = mb.createModbusArray(conc_and_dateTime);     // format the data for modbus (turning floats to ints) 


            ushort[] DateTime = mb.modbusDateTimeArray(modbusDataArray);
            ushort[] telemetryData = mb.ModbusTelemetryArray(instrumentData);      // here are the five arrays we'll want to write to modbus 
            ushort[] atomicNumbers = mb.atomicNumberArray(modbusDataArray);        // registers 
            ushort[] concArray = mb.modbusConcArray(modbusDataArray);              
            ushort[] uncArray = mb.modbusUncArray(modbusDataArray);             
            

            // initialize the modbus stuff
            DataStore xactHoldingRegisters;
            string modbusPort = "COM1";
            mb.initialize(modbusPort, 2400, out xactHoldingRegisters);

            int startAddress = 100;

            Console.WriteLine("Hit Enter to update registers.");
            Console.ReadLine(); 


            //update the registers
            mb.updateModbusSlaveRegisters(xactHoldingRegisters, atomicNumbers, startAddress);
            startAddress += 100; 
            mb.updateModbusSlaveRegisters(xactHoldingRegisters, concArray, startAddress);
            mb.updateModbusSlaveRegisters(xactHoldingRegisters, uncArray, startAddress +concArray.Length);

            startAddress = 701;
            mb.updateModbusSlaveRegisters(xactHoldingRegisters, DateTime, startAddress);
            startAddress += 2; 
            mb.updateModbusSlaveRegisters(xactHoldingRegisters, telemetryData, startAddress + DateTime.Length);

            
            ushort[] mbUpdate = new ushort[1] { 1 };
            mb.updateModbusSlaveRegisters(xactHoldingRegisters, mbUpdate, 700);


            Console.ReadLine();

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
        /// This method looks in the process_data directory and
        /// retrieves the most recent data file 
        /// </summary>
        /// <param name="path">
        /// The path to the process_data directory
        /// </param>
        /// <returns>
        /// A string containing the file's path
        /// </returns>
        public string findProcessFile(string path)
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
            return path+"\\"+targetFile;
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
        public void getRawData(string filename, out List<string> conc_and_dateTime, out List<string> instrumentData)
        {
            string[] rawData = System.IO.File.ReadAllLines(filename);

            conc_and_dateTime = new List<string>();
            instrumentData = new List<string>();

            // find Brian's index in process_data file
            var index = Array.IndexOf(rawData, ",*****,");


            //get datetime format for ADAPT 
            string[] line1 = rawData[index + 1].Split(',');
            string[] dateTime = line1[2].Split('-', ' ', ':', '/');
            conc_and_dateTime.Add(dateTime[2] + dateTime[0] + dateTime[1] + dateTime[3] + dateTime[4] + dateTime[5]);



            // get the concentrations 
            for (int i = 2; i < index; i++)
            {
                string[] splitLine = rawData[i].Split(',');
                conc_and_dateTime.Add(splitLine[0]); //symbol
                conc_and_dateTime.Add(splitLine[1]); // atomic number
                conc_and_dateTime.Add(splitLine[2]); //  mass
                conc_and_dateTime.Add(splitLine[3]); // uncertainty in mass
                conc_and_dateTime.Add(splitLine[4]); // concentration
            }

            // get the instrument data
            for (int i = index + 2; i < rawData.Length; i++)
            {
                string[] splitLine = rawData[i].Split(','); 

                instrumentData.Add(splitLine[2]);
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
        public ushort[] createModbusArray(List<string> data)
        {
            int numberOfAnalytes = (data.Count-1)/5;
            int dataLength = numberOfAnalytes * 5 +3; //3 for datetime, 1 per atomic #, 2 per conc, 2 per unc

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

            modbusDataArray[0] = (ushort)yearForModbus;
            modbusDataArray[1] = (ushort)monthDayForModbus;
            modbusDataArray[2] = (ushort)hoursMinutesForModbus;

            /*--------------------------------------------------------*/

            // get atomic #s, concentrations, and uncertainties

            ushort[] atomicNumbers = new ushort[numberOfAnalytes];
            int counter1 = 0; 
            for (int i = 2; i < data.Count; i+=5)
            {
                atomicNumbers[counter1] = ushort.Parse(data[i]);
                counter1 += 1; 
            }

            ushort[] concentrations = new ushort[numberOfAnalytes * 2];
            int counter2 = 0;
            for (int i = 5; i < data.Count; i+=5)
            {
                ushort[] conc = floatToIntegersForRegisters(float.Parse(data[i]));
                concentrations[counter2] = conc[0];
                concentrations[counter2 + 1] = conc[1];
                counter2 += 2; 
            }

            ushort[] uncertainties = new ushort[numberOfAnalytes * 2];
            int counter3 = 0;
            for (int i = 4; i < data.Count; i += 5)
            {
                ushort[] unc = floatToIntegersForRegisters(float.Parse(data[i]));
                uncertainties[counter3] = unc[0];
                uncertainties[counter3 + 1] = unc[1];
                counter3 += 2; 

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
        public ushort[] modbusDateTimeArray(ushort[] modbusArray)
        {
            ushort[] modbusDateTimeArray = new ushort[3];
            modbusDateTimeArray[0] = modbusArray[0];
            modbusDateTimeArray[1] = modbusArray[1];
            modbusDateTimeArray[2] = modbusArray[2];

            return modbusDateTimeArray; 
        }


        /// <summary>
        /// Creates a ushort array of ints with the atomic 
        /// numbers. 
        /// </summary>
        /// <param name="modbusArray"></param>
        /// <returns></returns>
        public ushort[] atomicNumberArray(ushort[] modbusArray)
        {
            int numberOfAnalytes = (modbusArray.Length - 3) / 5;
            ushort[] atomicNumberArray = new ushort[numberOfAnalytes];
            int counter = 0; 

            for (int i = 3; i < numberOfAnalytes+3; i++)
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
        public ushort[] modbusConcArray(ushort[] modbusArray)
        {
            int numberOfAnalytes = (modbusArray.Length - 3) / 5;
            ushort[] modbusConcArray = new ushort[numberOfAnalytes*2];
            int counter = 0; 

            for (int i = 3+numberOfAnalytes; i < 3+numberOfAnalytes*3; i++)
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
        public ushort[] modbusUncArray(ushort[] modbusArray)
        {
            int numberOfAnalytes = (modbusArray.Length - 3) / 5;
            ushort[] modbusUncArray = new ushort[numberOfAnalytes * 2];
            int counter = 0;

            for (int i = 3 + numberOfAnalytes*3; i < 3 + numberOfAnalytes * 5; i++)
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
        public ushort[] ModbusTelemetryArray(List<string> data)
        {
            ushort[] returnData = new ushort[(data.Count-1) * 2];
            int counter = 0;
            for (int i = 1; i < data.Count; i++)
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
        public void updateModbusSlaveRegisters(DataStore dataStore, ushort[] dataToWrite, int startAddress)
        {


            for (int i = 0; i < dataToWrite.Length; i++) //we want to start at address = 1 to make modbus happy but need to get all the values written 
            {

                dataStore.HoldingRegisters[startAddress] = dataToWrite[i];
                startAddress++;
            }

        }


    }

    class adaptDB
    {

    }

    class gillWindSensor
    {

    }

    class gillWindSensorUtilities
    {

    }
}
