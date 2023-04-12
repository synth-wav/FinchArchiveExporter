using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.CodeDom;
using System.Text.RegularExpressions;

namespace FinchArchiveExporter {
    class ParseArchive {

        public const int SEPARATOR_LENGTH = 50;
        public const int CREATION_TIME_OFFSET = 21;
        public const int ENTRY_BODY_OFFSET = 24;
        public const int SKIP_OFFSET = 43;
        public const String BULLET_TYPE_2 = "\"bullet_type\":2";
        static void Main(string[] args) {
            String fileName;
            List<Entry> entries = new List<Entry>();
            if (args.Length == 0 || args.Length > 1) {
                Console.WriteLine("Usage: FinchArchiveParser inputFile.json");
            }
            String line = "";
            fileName = args[0];
            try {
                line = File.ReadLines(fileName).First();
                //Console.WriteLine(line);
            }
            catch (Exception e) {
                Console.WriteLine("Error opening file {0}", args[0]);
                Console.WriteLine(e.Message);
            }
            int nextBulletPos = line.IndexOf("\"bullet_type\":2");
            String lineTrimmed = "";
            if (nextBulletPos > 0) {
                lineTrimmed = line.Substring(nextBulletPos);
            }
            else
            {
                Console.WriteLine("The file does not contain any journal entries");
                System.Environment.Exit(1);
            }
            while(nextBulletPos > 0) {
                String entryBody = lineTrimmed.Substring(ENTRY_BODY_OFFSET, (lineTrimmed.IndexOf("\",") - ENTRY_BODY_OFFSET));

                //Remove these characters in the body, they're used for the sentiment/subject analysis stuff
                // but we don't want to see them in our export
                entryBody = entryBody.Replace("#","");
                entryBody = entryBody.Replace("_", " ");

                //Fixing a strange output formatting error where "\n\n" in plaintext appears in the body text
                entryBody = entryBody.Replace("\\n\\n", "");
                int charSinceSpace = 0;
                int charCount = 0;
                
                foreach (char c in entryBody)
                {
                    //Padding the entry body text for readability
                    if (c == ' ') {
                        if (charSinceSpace > 50)
                        {
                            entryBody = entryBody.Insert(charCount, "\n");
                            entryBody = entryBody.Remove(charCount + 1, 1);
                            charSinceSpace = 0;
                        }
                    }
                    //Conditional removing of exclamation points if they aren't followed by a space
                    //As these are also included for emotion identification in your entries.
                    //I don't know if this rule applies in other languages but I always have a space
                    //after an '!' so that's how it's going to work
                    if (c == '!')
                    {
                        //Console.WriteLine("{0}, {1}", entryBody.Length - 1, charCount);
                        if (charCount != entryBody.Length && charCount != entryBody.Length - 1 && entryBody[charCount + 1] != ' ')
                        {
                            entryBody = entryBody.Remove(charCount, 1);
                            charCount--;
                        }
                    }
                    charSinceSpace++;
                    charCount++;
                }

                //Grab the time created
                String entryTimeString = lineTrimmed.Substring(lineTrimmed.IndexOf("creation_time")
                    + CREATION_TIME_OFFSET, CREATION_TIME_OFFSET - 1);
                
                //Remove extra quote if date is shorter
                entryTimeString = entryTimeString.Replace("\"", "");
                
                //Parse the DateTime from the substring
                DateTime entryTime = DateTime.Parse(entryTimeString);

                //Console.WriteLine("Text body is: {0}", entryBody);
                //Console.WriteLine("Time string is {0}", entryTimeString);
                //Console.WriteLine("DateTime is {0}", entryTime.ToString());

                //Create a new instance of Entry with the time and body text and add it to our list of Entries
                Entry currentEntry = new Entry(entryTime, entryBody);
                entries.Add(currentEntry);

                //Move forward to the next reflection entry
                line = lineTrimmed.Substring(line.IndexOf("creation_time") + SKIP_OFFSET);
                nextBulletPos = line.IndexOf(BULLET_TYPE_2);
                if(nextBulletPos > 0)
                    lineTrimmed = line.Substring(nextBulletPos);
            }

            //Sort the entries by their entryTime
            entries.Sort();
            //Create output file
            using (FileStream fs = File.Create("FinchArchiveExport" + DateTime.Now.ToString("MM-dd-yyyy-hh-mm-ss") + ".txt"))
            {
                using (var fw = new StreamWriter(fs))
                {
                    //Write header
                    fw.WriteLine("Finch Journal Entry Export " + DateTime.Now.ToString("D"));
                    fw.WriteLine(string.Concat(Enumerable.Repeat("-", SEPARATOR_LENGTH)) + "\n");

                    //Write the earliest date in the entry list at the top
                    DateTime currentDate = entries[0].entryTime.Date;
                    fw.WriteLine(currentDate.ToString("D"));
                    fw.WriteLine(string.Concat(Enumerable.Repeat("-", SEPARATOR_LENGTH)));

                    //Write each entry
                    foreach (Entry entry in entries)
                    {
                        //Update and print the current date if the current entry is on a new day
                        if (entry.entryTime.Date > currentDate)
                        {
                            currentDate = entry.entryTime.Date;
                            fw.WriteLine("\n" + currentDate.AddDays(-1).ToString("D"));
                            //Console.WriteLine("\n" + currentDate.ToString("D") + "\n");
                            fw.WriteLine(string.Concat(Enumerable.Repeat("-", SEPARATOR_LENGTH)));
                        }
                        //Write the current entry
                        fw.WriteLine("{0}: {1}\n", entry.entryTime.ToShortTimeString(), entry.entryBody);
                        //Console.WriteLine("{0}: {1}", entry.entryTime.ToShortTimeString(), entry.entryBody);
                    }
                }
            }
            System.Environment.Exit(0);
        }
    }

    class Entry : IComparable<Entry> {
        //The creation time of the entry
        public DateTime entryTime;

        //The body text of the entry
        public String entryBody;

        public Entry(DateTime myEntryTime, String myEntryBody) {
            this.entryTime = myEntryTime;
            this.entryBody = myEntryBody;
        }

        //Compares entries based on their creation time
        int IComparable<Entry>.CompareTo(Entry other)
        {
            return DateTime.Compare(this.entryTime, other.entryTime);
        }
    }
}
