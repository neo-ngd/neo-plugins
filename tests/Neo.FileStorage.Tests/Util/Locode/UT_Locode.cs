using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.Database;
using Neo.FileStorage.Database.LevelDB;
using Neo.FileStorage.Utils;
using Neo.FileStorage.Utils.Locode;
using Neo.FileStorage.Utils.Locode.Db;
using Neo.IO;

namespace Neo.FileStorage.Tests.Util.Locode
{
    [TestClass]
    public class UT_Locode
    {
        private class TestLOCODEStorage : IDisposable
        {
            private const byte PreLocode = 0x00;
            private readonly IDB _db;

            public TestLOCODEStorage(string path)
            {
                _db = new DB(path);
            }

            public void Dispose()
            {
                _db.Dispose();
            }

            public void Iterate(Func<Key, Record, bool> handler)
            {
                _db.Iterate(new byte[] { PreLocode }, (key, value) =>
                   {
                       return handler(key[1..].AsSerializable<Key>(), value.AsSerializable<Record>());
                   });
            }
        }

        [TestMethod]
        public void TestFillDatabase()
        {
            string resourcePath = "./Resources/";
            string[] tableInPaths = new string[]
            {
                resourcePath + "2020-2 UNLOCODE CodeListPart1.csv",
                resourcePath + "2020-2 UNLOCODE CodeListPart2.csv",
                resourcePath + "2020-2 UNLOCODE CodeListPart3.csv",
            };
            string tableSubDivPath = resourcePath + "2020-2 SubdivisionCodes.csv";
            string airportsPath = resourcePath + "airports.dat";
            string countriesPath = resourcePath + "countries.dat";
            string continentsPath = resourcePath + "continents.geojson";
            CSVTable locodeDB = new(tableInPaths, tableSubDivPath);
            AirportsDB airportsDB = new()
            {
                AirportsPath = airportsPath,
                CountriesPath = countriesPath
            };
            ContinentDB continentDB = new()
            {
                Path = continentsPath
            };
            string dbPath = "./Data_LOCODE";
            using StorageDB targetDb = new(dbPath);
            targetDb.FillDatabase(locodeDB, airportsDB, continentDB);
            Directory.Delete(dbPath, true);
        }

        [TestMethod]
        public void TestReadCountries()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };
            using (var reader = new StreamReader("./Resources/countries.dat"))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<AirportsDB.Country>();
                Assert.AreEqual(records.Count(), 261);
            }
        }

        [TestMethod]
        public void TestReadEmptyRecord()
        {
            string path = "test.csv";
            string content = ",\"AD\",,\".ANDORRA\",,,,,,,,";
            File.WriteAllText(path, content);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<CSVTable.UNLOCODERecord>();
            var record = records.First();
            Assert.AreEqual("AD", record.CountryCode);
            File.Delete(path);
        }

        [TestMethod]
        public void TestReadUNLOCODE()
        {
            string path = "./Resources/2020-2 UNLOCODE CodeListPart1.csv";
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<CSVTable.UNLOCODERecord>();
            Assert.AreEqual(50990, records.Count());
        }

        [TestMethod]
        public void TestContinents()
        {
            string continentsPath = "./Resources/continents.geojson";
            ContinentDB continentDB = new()
            {
                Path = continentsPath
            };
            Assert.AreEqual(Continent.ContinentEurope, continentDB.PointContinent(new() { Latitude = 48.25, Longitude = 15.45 }));
        }
    }
}
