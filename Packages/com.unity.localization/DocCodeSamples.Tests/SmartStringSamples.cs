using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using UnityEngine.Localization.Tables;
using Random = UnityEngine.Random;

public class SmartStringSamples
{
    public void MarkSmart()
    {
        #region mark-smart

        // Get the collection.
        var collection = LocalizationEditorSettings.GetStringTableCollection("My Strings");

        // Get the English table.
        var englishTable = collection.GetTable("en") as StringTable;

        // Get the Entry we want to mark as Smart.
        var entry = englishTable.GetEntry("My Entry");
        entry.IsSmart = true;

        #if UNITY_EDITOR
        // If we are in the Editor then we need to mark the table dirty so the changes are saved.
        EditorUtility.SetDirty(englishTable);
        #endif
        #endregion
    }

    public void MultipleArguments()
    {
        #region multiple-arguments

        var localizedString = new LocalizedString("My Table", "My Table Entry");

        var dict1 = new Dictionary<string, string>() { { "Name", "John" } };
        var dict2 = new Dictionary<string, string>() { { "City", "Washington" } };

        localizedString.Arguments = new object[] { dict1, dict2 };

        Debug.Log("The value is: " + localizedString.GetLocalizedString());
        #endregion
    }

    public void SourceOrderExample()
    {
        #region source-order

        var localizedString = new LocalizedString("My Table", "My Table Entry");
        var dict = new Dictionary<string, string> { { "Count", "Hello World" } };
        localizedString.Arguments = new object[] { dict };
        #endregion
    }

    public void DictionaryExample()
    {
        #region dictionary-source

        var localizedString = new LocalizedString("My Table", "My Table Entry");
        var dict = new Dictionary<string, string> { { "Name", "Gordon" }, { "Surname", "Freeman" } };
        localizedString.Arguments = new object[] { dict };
        #endregion
    }

    public static void DefaultSourceArguments()
    {
        var localizedString = new LocalizedString();

        localizedString.Arguments = new object[]
        {
            #region args-one-two-three
            1, 2, 3
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-string-int
            "Potato", 155
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-anon-class
            new { Name = "One", Points = 100 }
            #endregion
        };
    }

    public static void DictionarySourceArguments()
    {
        #region args-dict-example
        var localizedString = new LocalizedString("My Table", "My Table Entry");
        var dict = new Dictionary<string, string> { { "Name", "Gordon" }, { "Surname", "Freeman" } };
        localizedString.Arguments = new object[] { dict };
        #endregion

        localizedString.Arguments = new object[]
        {
            #region args-dict-1
            new Dictionary<string, object>() { { "SomeKey", 999 } }
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-dict-2
            new Dictionary<string, string>(){ { "Name", "Gordon" }, { "Surname", "Freeman" }, }
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-dict-3
            new Dictionary<string, string>(){ { "Name", "Gordon" }, { "Surname", "Freeman" }, },
            new Dictionary<string, object>() { { "Score", 153 } }
            #endregion
        };
    }

    public static void ListSourceArguments()
    {
        var localizedString = new LocalizedString();

        localizedString.Arguments = new object[]
        {
            #region args-list-1
            new List<int> { 1, 2, 3, 4 }
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-list-2
            new List<object> { 1, "Hello", "World" }
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-list-3
            new List<int> { 1, 2, 3, 4 },
            new List<string> { "one", "two", "three", "four" }
            #endregion
        };
    }

    #region args-reflection-3a
    public class Person
    {
        public int Age = 39;
        public string Name = "Juan Pérez";
    }
    #endregion

    public static void ReflectionSourceArguments()
    {
        var localizedString = new LocalizedString();

        var gameObject = new GameObject("Camera", typeof(Camera));
        localizedString.Arguments = new object[]
        {
            #region args-reflection-1
            gameObject.GetComponent<Camera>()
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-reflection-2
            "Hello World"
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-reflection-3b
            new Person()
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-reflection-4
            new { name = "Yamada", surname = "Taro" }
            #endregion
        };

        #region args-reflection-5
        var myLocalizedString = new LocalizedString("My Table", "My Entry");

        var person = new Person { Name = "Michael Scott", Age = 40 };
        myLocalizedString.Arguments = new object[] { person };

        Debug.Log(myLocalizedString.GetLocalizedString());
        #endregion
    }

    public static void TupleSourceArguments()
    {
        var localizedString = new LocalizedString();

        localizedString.Arguments = new object[]
        {
            #region args-tuple-1
            (new Person { Name = "Juan" }, new Dictionary<string, string> { { "Key", "Value" } })
            #endregion
        };
    }

    public static void ConditionalFormatterArguments()
    {
        var localizedString = new LocalizedString();

        localizedString.Arguments = new object[]
        {
            #region args-cond-1
            new { Name = "Jim", Age = 32, Address = new { City = "New York", State = "NY" } }
            #endregion
        };
    }

    public static void IsMatchFormatterArguments()
    {
        var localizedString = new LocalizedString();

        localizedString.Arguments = new object[]
        {
            #region args-ismatch-1
            new Dictionary<string, object>() { {"theKey", "Some123Content"}}
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-ismatch-2
            new List<int> {100, 200, 300}
            #endregion
        };
    }

    public static void ListFormatterArguments()
    {
        var localizedString = new LocalizedString();

        localizedString.Arguments = new object[]
        {
            #region args-list-formatter-1
            new[] { "one", "two", "three" }
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-list-formatter-2
            new Dictionary<string, object>() { {"theKey", "Some123Content"}}
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-list-formatter-3
            new List<int> {100, 200, 300}
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-list-formatter-4
            new List<int> { 1, 2, 3, 4 },
            new List<string> { "one", "two", "three", "four" }
            #endregion
        };

        #region args-list-formatter-loc-string-example
        // Text: "Use {0:list:{}|, |, and } to move".
        var myLocalizedString = new LocalizedString("My Table", "My Entry");
        myLocalizedString.Arguments = new object[] { new object[] { "w", "a", "s", "d" } };

        // Outputs "Use w, a, s and d to move"
        Debug.Log(myLocalizedString.GetLocalizedString(myLocalizedString));
        #endregion
    }

    public static void PluralFormatterArguments()
    {
        var localizedString = new LocalizedString();

        localizedString.Arguments = new object[]
        {
            #region args-plural-formatter-1
            new[] { "Bob", "Alice" }
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-plural-formatter-2
            new[] { "Mohamed" }
            #endregion
        };
    }

    public static void SubStringFormatterArgument()
    {
        var localizedString = new LocalizedString();

        localizedString.Arguments = new object[]
        {
            #region args-substring-formatter-1
            new { name = "Lara", surname = "Croft" }
            #endregion
        };
    }

    public static void TemplateFormatterArgument()
    {
        var localizedString = new LocalizedString();

        localizedString.Arguments = new object[]
        {
            #region args-template-formatter-1
            new { name = "Lara", surname = "Croft" }
            #endregion
        };
    }

    public static void TimeFormatterArgument()
    {
        var localizedString = new LocalizedString();

        localizedString.Arguments = new object[]
        {
            #region args-time-formatter-1
            new TimeSpan(450, 30, 22, 5, 0)
            #endregion
        };

        localizedString.Arguments = new object[]
        {
            #region args-time-formatter-2
            new TimeSpan(0, 30, 22, 5, 0)
            #endregion
        };
    }
}

#region person-sample

public class Address
{
    public string StreetAddress { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
}

public class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }
    public string FullName { get; set; }
    public string Name => FirstName + " " + LastName;
    public DateTime Birthday { get; set; }

    public int Age
    {
        get
        {
            if (Birthday.Month < DateTime.Now.Month || (Birthday.Month == DateTime.Now.Month && Birthday.Day <= DateTime.Now.Day))
            {
                return DateTime.Now.Year - Birthday.Year;
            }
            else
            {
                return DateTime.Now.Year - 1 - Birthday.Year;
            }
        }
    }

    public Address Address { get; set; }

    public List<Person> Friends { get; set; }
    public int NumberOfFriends => this.Friends.Count;
    public Person Spouse { get; set; }
}
#endregion
