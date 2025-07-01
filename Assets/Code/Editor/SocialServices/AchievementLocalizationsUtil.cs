using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Awaken.TG.Editor.SocialServices {
    public class AchievementLocalizationsUtil {
        public static List<LocalizedStrings> ImportLocalizations(string inputTranslationsPath) {
            using var stream = File.OpenRead(Path.Combine(inputTranslationsPath));
            using var reader = new StreamReader(stream);
            // using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            // if (csv.Read() && csv.ReadHeader()) {
            //     var dataMap = new LocalizedTrophyImportMap();
            //     csv.Context.RegisterClassMap(dataMap);
            //     return csv.GetRecords<LocalizedStrings>().ToList();
            // } else {
            //     throw new NullReferenceException("No header found in CSV file.");
            // }
            throw new NotImplementedException();
        }
    }
            
    [UsedImplicitly]
    public class LocalizedStrings {
        public string eng;
        public string fr;
        public string de;
        public string es;
        public string it;
        public string ja;
        public string cns;
        public string cnt;
        public string cz;
        public string pl;
        public string pt;
        public string ru;
    }

    // sealed class LocalizedTrophyImportMap : ClassMap<LocalizedStrings> {
    //     public LocalizedTrophyImportMap() {
    //         Map(m => m.eng).Index(0).Name("EN Achievement Name & Desc");
    //         Map(m => m.ja).Index(1).Name("Japanese (ja)");
    //         Map(m => m.it).Index(2).Name("Italian (it)");
    //         Map(m => m.de).Index(3).Name("German (de)");
    //         Map(m => m.fr).Index(4).Name("French (fr)");
    //         Map(m => m.cnt).Index(5).Name("Chinese (Traditional) (zh-TW)");
    //         Map(m => m.cns).Index(6).Name("Chinese (Simplified) (zh-Hans)");
    //         Map(m => m.cz).Index(7).Name("Czech (cs)");
    //         Map(m => m.es).Index(8).Name("Spanish (es)");
    //         Map(m => m.pt).Index(9).Name("Portuguese (Brazil) (pt-BR)");
    //         Map(m => m.ru).Index(10).Name("Russian (ru)");
    //         Map(m => m.pl).Index(11).Name("Polish (pl)");
    //     }
    // }
}