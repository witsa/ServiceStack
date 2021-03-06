﻿using System;
using System.Collections.Generic;
using System.Configuration;
using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;

namespace ServiceStack.Common.Tests
{
    public class OrmLiteAppSettingsTest : AppSettingsTest
    {
        private OrmLiteAppSettings settings;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            settings = new OrmLiteAppSettings(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            settings.InitSchema();
        }

        public override AppSettingsBase GetAppSettings()
        {
            var testConfig = (DictionarySettings)base.GetAppSettings();

            using (var db = settings.DbFactory.Open())
            {
                db.DeleteAll<ConfigSetting>();

                foreach (var config in testConfig.GetAll())
                {
                    settings.Set(config.Key, config.Value);
                }
            }

            return settings;
        }

        [Test]
        public void GetString_returns_null_On_Nonexistent_Key()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.GetString("GarbageKey");
            Assert.IsNull(value);
        }

        [Test]
        public void GetList_returns_emtpy_list_On_Null_Key()
        {
            var appSettings = GetAppSettings();

            var result = appSettings.GetList("GarbageKey");

            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Does_GetOrCreate_New_Value()
        {
            var appSettings = (OrmLiteAppSettings)GetAppSettings();

            var i = 0;

            var key = "key";
            var result = appSettings.GetOrCreate(key, () => key + ++i);
            Assert.That(result, Is.EqualTo("key1"));

            result = appSettings.GetOrCreate(key, () => key + ++i);
            Assert.That(result, Is.EqualTo("key1"));
        }
    }

    public class DictionarySettingsTest : AppSettingsTest
    {
        [Test]
        public void GetString_Throws_Exception_On_Nonexistent_Key()
        {
            var appSettings = GetAppSettings();
            try
            {
                appSettings.GetString("GarbageKey");
                Assert.Fail("GetString did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("GarbageKey"));
            }
        }

        [Test]
        public void GetList_Throws_Exception_On_Null_Key()
        {
            var appSettings = GetAppSettings();
            try
            {
                appSettings.GetList("GarbageKey");
                Assert.Fail("GetList did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("GarbageKey"));
            }
        }
    }

    public abstract class AppSettingsTest
    {
        public virtual AppSettingsBase GetAppSettings()
        {
            return new DictionarySettings(new Dictionary<string, string>
            {
                {"NullableKey", null},
                {"EmptyKey", string.Empty},
                {"RealKey", "This is a real value"},
                {"ListKey", "A,B,C,D,E"},
                {"IntKey", "42"},
                {"BadIntegerKey", "This is not an integer"},
                {"DictionaryKey", "A:1,B:2,C:3,D:4,E:5"},
                {"BadDictionaryKey", "A1,B:"},
                {"ObjectNoLineFeed", "{SomeSetting:Test,SomeOtherSetting:12,FinalSetting:Final}"},
                {"ObjectWithLineFeed", "{SomeSetting:Test,\r\nSomeOtherSetting:12,\r\nFinalSetting:Final}"},
            }) {
                ParsingStrategy = null,   
            };
        }

        [Test]
        public void GetNullable_String_Returns_Null()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.GetNullableString("NullableKey");

            Assert.That(value, Is.Null);
        }

        [Test]
        public void GetString_Returns_Value()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.GetString("RealKey");

            Assert.That(value, Is.EqualTo("This is a real value"));
        }

        [Test]
        public void Get_Returns_Default_Value_On_Null_Key()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.Get("NullableKey", "default");

            Assert.That(value, Is.EqualTo("default"));
        }

        [Test]
        public void Get_Casts_To_Specified_Type()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.Get<int>("IntKey", 1);

            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Get_Throws_Exception_On_Bad_Value()
        {
            var appSettings = GetAppSettings();

            try
            {
                appSettings.Get<int>("BadIntegerKey", 1);
                Assert.Fail("Get did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("BadIntegerKey"));
            }
        }

        [Test]
        public void GetList_Parses_List_From_Setting()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.GetList("ListKey");

            Assert.That(value, Has.Count.EqualTo(5));
            Assert.That(value, Is.EqualTo(new List<string> { "A", "B", "C", "D", "E" }));
        }

        [Test]
        public void GetDictionary_Parses_Dictionary_From_Setting()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.GetDictionary("DictionaryKey");

            Assert.That(value, Has.Count.EqualTo(5));
            Assert.That(value.Keys, Is.EqualTo(new List<string> { "A", "B", "C", "D", "E" }));
            Assert.That(value.Values, Is.EqualTo(new List<string> { "1", "2", "3", "4", "5" }));
        }

        [Test]
        public void GetDictionary_Throws_Exception_On_Null_Key()
        {
            var appSettings = GetAppSettings();

            try
            {
                appSettings.GetDictionary("GarbageKey");
                Assert.Fail("GetDictionary did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("GarbageKey"));
            }
        }

        [Test]
        public void GetDictionary_Throws_Exception_On_Bad_Value()
        {
            var appSettings = GetAppSettings();

            try
            {
                appSettings.GetDictionary("BadDictionaryKey");
                Assert.Fail("GetDictionary did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("BadDictionaryKey"));
            }
        }
        
        [Test]
        public void Get_Returns_ObjectNoLineFeed()
        {
            var appSettings = GetAppSettings();
            appSettings.ParsingStrategy = AppSettingsStrategy.CollapseNewLines;
            var value = appSettings.Get("ObjectNoLineFeed", new SimpleAppSettings());
            Assert.That(value, Is.Not.Null);
            Assert.That(value.FinalSetting, Is.EqualTo("Final"));
            Assert.That(value.SomeOtherSetting, Is.EqualTo(12));
            Assert.That(value.SomeSetting, Is.EqualTo("Test"));
        }

        [Test]
        public void Get_Returns_ObjectWithLineFeed()
        {
            var appSettings = GetAppSettings();
            appSettings.ParsingStrategy = AppSettingsStrategy.CollapseNewLines;
            var value = appSettings.Get("ObjectWithLineFeed", new SimpleAppSettings());
            Assert.That(value, Is.Not.Null);
            Assert.That(value.FinalSetting, Is.EqualTo("Final"));
            Assert.That(value.SomeOtherSetting, Is.EqualTo(12));
            Assert.That(value.SomeSetting, Is.EqualTo("Test"));
        }

        public class SimpleAppSettings
        {
            public string SomeSetting { get; set; }
            public int SomeOtherSetting { get; set; }
            public string FinalSetting { get; set; }
        }
    }
}
