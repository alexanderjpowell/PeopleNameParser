using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace PeopleNameParser
{
    public class ParsedPersonName
    {
        public string prefix;
        public string firstname;
        public string middlename;
        public string lastname;
        public string suffix;
        public string gender;

        // Global vars
        string[] input_array;
        bool hasComma;
        int length;

        public ParsedPersonName(string input)
        {
            if (input == null) { input = ""; }
            prefix = firstname = middlename = lastname = suffix = gender = "";
            parseName(input); // Parse the name
            populatePrefixAndSuffixDictionaries();

            // Case the name components
            firstname = caseName(firstname).Trim();
            middlename = caseName(middlename).Trim();
            lastname = caseName(lastname).Trim();
            prefix = casePrefixes(prefix).Trim();
            suffix = caseSuffixes(suffix).Trim();

            // Attempt to determine the gender
            // Note: this is very conservative and gives Unknown for many names
            guessGender(firstname.ToUpper());
        }

        // Create string arrays of text files from resources
        private static string[] allMaleNames = PeopleNameParser.Properties.Resources.male_names.Split(null);
        private static string[] allFemaleNames = PeopleNameParser.Properties.Resources.female_names.Split(null);
        private static string[] allSurNames = PeopleNameParser.Properties.Resources.surnames.Split(null);
        private static string[] allPrefixes = PeopleNameParser.Properties.Resources.prefixes.Split(null);
        private static string[] allSuffixes = PeopleNameParser.Properties.Resources.suffixes.Split(null);

        // Set up Hash sets
        private static HashSet<string> maleNames = new HashSet<string>(allMaleNames);
        private static HashSet<string> femaleNames = new HashSet<string>(allFemaleNames);
        private static HashSet<string> surNames = new HashSet<string>(allSurNames);
        private static HashSet<string> prefixes = new HashSet<string>(allPrefixes);
        private static HashSet<string> suffixes = new HashSet<string>(allSuffixes);

        // Set up dictionaries
        Dictionary<string, string> SuffixDictionary = new Dictionary<string, string>();
        Dictionary<string, string> PrefixDictionary = new Dictionary<string, string>();


        public void parseName(string inputName)
        {
            string orig = inputName;
            inputName = inputName.ToLower();
            //Console.WriteLine("Before: " + inputName);
            inputName = cleanUpString(inputName);
            //Console.WriteLine("After: " + inputName);
            hasComma = inputName.Contains(',');
            if (hasComma)
            {
                try { inputName = inputName.Replace(",", ", "); }
                catch { inputName = orig; }
            }
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            inputName = rgx.Replace(inputName, "");
            input_array = inputName.Trim().ToUpper().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            length = input_array.Length; // Length of the name array

            switch (length)
            {
                case 0:
                    break;
                case 1:
                    firstname = input_array[0];
                    break;
                case 2:
                    parseTwoTokenName();
                    break;
                case 3:
                    parseThreeTokenName();
                    break;
                case 4:
                    parseFourTokenName();
                    break;
                case 5:
                    parseFiveTokenName();
                    break;
                case 6:
                    parseSixTokenName();
                    break;
                default:
                    parseGreaterThanSixTokens();
                    break;
            }

            if (prefix == "" && firstname == "" && middlename == "" && lastname == "" && suffix == "")
            {
                riskyParse(); // Might as well attempt to parse the name if existing algorithm didn't work
            }
        }

        public string getPrefix() { return prefix; }
        public string getFirstName() { return firstname; }
        public string getMiddleName() { return middlename; }
        public string getLastName() { return lastname; }
        public string getSuffix() { return suffix; }
        public string getGender() { return gender; }

        public string cleanUpString(string input)
        {
            input = input.Trim();
            input = input.ToUpper();
            // Fix names where the user entered a zero instead of letter O
            if (input.Contains(" 0 '"))
            {
                input = input.Replace(" 0 '", " O'");
            }
            if (input.Contains(" 0'"))
            {
                input = input.Replace(" 0'", " O'");
            }
            if (input.Contains(" 0 "))
            {
                input = input.Replace(" 0 ", " O ");
            }

            // Remove anything between parentheses or brackets
            Regex rgx1 = new Regex("\\(.*?\\)");
            Regex rgx2 = new Regex("\\[.*?\\]");
            input = rgx1.Replace(input, string.Empty);
            input = rgx2.Replace(input, string.Empty);

            //Console.WriteLine("here: " + input);

            if (input.Contains(", M D"))
            {
                input = input.Replace(", M D", ", MD");
            }
            else if (input.EndsWith(" M D"))
            {
                input.Replace(" M D", " MD");
            }
            else if (input.EndsWith(" M D."))
            {
                input.Replace(" M D.", " MD.");
            }

            if (input.Contains(", PH D"))
            {
            	input = input.Replace(", PH D", ", PHD");
            }
            else if (input.Contains(" PH D"))
            {
            	input = input.Replace(" PH D", " PHD");
            }
            else if (input.Contains(",PH D"))
            {
            	input = input.Replace(",PH D", ", PHD");
            }

            // Remove any numerics from string
            if (input == null) { input = ""; }
            try
            {
                input = input.Trim();
                int indexOfFirstDigit = input.IndexOfAny("0123456789".ToCharArray());
                if (indexOfFirstDigit < 6) { }
                else
                {
                    input = input.Substring(0, indexOfFirstDigit);
                    input = input.Trim(); input.TrimEnd(',');
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                //return input.Trim();
            }

            try
            {
                if (input[input.Length - 1] == '-')
                {
                    input = input.Remove(input.Length - 1);
                }
            }
            catch (Exception ex) { /*Console.WriteLine(ex.Message);*/ }

            //string[] input_array = input.Trim().ToUpper().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            //foreach (string i in input_array)
            //{
            //    if (prefixes.Contains)
            //}

            // Join together names like 'de quesada' and 'van buren'
            string[] compoundWords = { " DE ", " VAN ", " MC ", " VON " };
            foreach (string i in compoundWords)
            {
                if (input.Contains(i))
                {
                    input = input.Remove(input.IndexOf(i) + i.Length - 1, 1);
                }
            }
            string[] compoundWords2 = { "DE ", "VAN ", "MC ", "VON " };
            foreach (string i in compoundWords2)
            {
                try
                {
                    if (input.Substring(0, i.Length) == i)
                    { // remove the space
                        input = input.Remove(i.Length - 1, 1);
                    }
                }
                catch { }
            }
            return input;
        }

        private void parseTwoTokenName()
        {
            if (hasComma)
            {
                firstname = input_array[1];
                lastname = input_array[0];
            }
            else
            {
                if ((input_array[1].Length == 1) && (input_array[0].Length > 1))
                {
                    firstname = input_array[1];
                    lastname = input_array[0];
                }
                else if ((maleNames.Contains(input_array[0]) || femaleNames.Contains(input_array[0]))
                    && (maleNames.Contains(input_array[1]) || femaleNames.Contains(input_array[1])))
                {
                    firstname = input_array[0];
                    lastname = input_array[1];
                }
                else if (maleNames.Contains(input_array[0]) || femaleNames.Contains(input_array[0]))
                {
                    firstname = input_array[0];
                    lastname = input_array[1];
                }
                else if (maleNames.Contains(input_array[1]) || femaleNames.Contains(input_array[1]))
                {
                    firstname = input_array[1];
                    lastname = input_array[0];
                }
                else
                {
                    firstname = input_array[0];
                    lastname = input_array[1];
                }
            }
        }

        private void parseThreeTokenName()
        {
            if ((input_array[0].Length == 1) && (input_array[1].Length == 1) && (!prefixes.Contains(input_array[0])))
            {
                firstname = input_array[0];
                middlename = input_array[1];
                lastname = input_array[2];
            }
            else if (suffixes.Contains(input_array[2]))
            { // F L S
                firstname = input_array[0];
                lastname = input_array[1];
                suffix = input_array[2];
            }
            else if (input_array[0].Length == 1)
            {
                if (suffixes.Contains(input_array[2]))
                {
                    firstname = input_array[0];
                    lastname = input_array[1];
                    suffix = input_array[2];
                }
                else
                {
                    firstname = input_array[0];
                    middlename = input_array[1];
                    lastname = input_array[2];
                }
            }
            else if (prefixes.Contains(input_array[0]))
            {
                prefix = input_array[0];
                firstname = input_array[1];
                lastname = input_array[2];
            }
            else if (maleNames.Contains(input_array[0]) || femaleNames.Contains(input_array[0]))
            {// first name is first
                // try to find last name
                if (surNames.Contains(input_array[1]))
                {
                    if (surNames.Contains(input_array[2]))
                    {
                        firstname = input_array[0];
                        middlename = input_array[1];
                        lastname = input_array[2];
                    }
                    else if (hasComma && input_array[2].Length == 1)
                    {// Last, First MiddleInitial
                        firstname = input_array[1];
                        middlename = input_array[2];
                        lastname = input_array[0];
                    }
                    else if (!hasComma && input_array[1].Length == 1)
                    {
                        firstname = input_array[0];
                        middlename = input_array[1];
                        lastname = input_array[2];
                    }
                    else
                    {
                        // lastname is second, assume suffix is third
                        firstname = input_array[0];
                        lastname = input_array[1];
                        suffix = input_array[2];
                    }
                }
                else if (surNames.Contains(input_array[2]))
                {// lastname is third, assume middle is second
                    firstname = input_array[0];
                    lastname = input_array[2];
                    middlename = input_array[1];
                }
                else if (suffixes.Contains(input_array[2]))
                {
                    firstname = input_array[0];
                    lastname = input_array[1];
                    suffix = input_array[2];
                }
                else
                {
                    firstname = input_array[0];
                    middlename = input_array[1];
                    lastname = input_array[2];
                }
            }
            else if (maleNames.Contains(input_array[1]) || femaleNames.Contains(input_array[1]))
            {// firstname is second
                if (surNames.Contains(input_array[2]))
                {
                    if (!hasComma)
                    {
                        // P F L
                        prefix = input_array[0];
                        firstname = input_array[1];
                        lastname = input_array[2];
                    }
                    else // There's a comma in the name
                    {
                        // P F L
                        prefix = input_array[0];
                        firstname = input_array[1];
                        lastname = input_array[2];
                    }
                }
                else if (surNames.Contains(input_array[0]))
                {// L, F M
                    lastname = input_array[0];
                    firstname = input_array[1];
                    middlename = input_array[2];
                }
                else if (surNames.Contains(input_array[1]))
                {// not sure if it's a first or last name
                    if (input_array[0].Length <= 2)
                    {
                        prefix = input_array[0];
                        firstname = input_array[1];
                        lastname = input_array[2];
                    }
                    else
                    {
                        firstname = input_array[0];
                        lastname = input_array[1];
                        suffix = input_array[2];
                    }
                }
                else
                {
                    //UNHANDLED
                }
            }
            else if (maleNames.Contains(input_array[2]) || femaleNames.Contains(input_array[2]))
            {// firstname is last (L S, F)
                if (surNames.Contains(input_array[0]))
                {
                    if (suffixes.Contains(input_array[1]))
                    {
                        lastname = input_array[0];
                        suffix = input_array[1];
                        firstname = input_array[2];
                    }
                    else if (prefixes.Contains(input_array[1]))
                    {
                        lastname = input_array[0];
                        prefix = input_array[1];
                        firstname = input_array[2];
                    }
                }
                else if (surNames.Contains(input_array[2]))
                {
                    firstname = input_array[0];
                    middlename = input_array[1];
                    lastname = input_array[2];
                }
                else
                {// UNHANDLED
                }
            }
            // could not find a first name - now check for last names
            else
            {
                if (prefixes.Contains(input_array[0]))
                {
                    prefix = input_array[0];
                    firstname = input_array[1];
                    lastname = input_array[2];
                }
                else if (suffixes.Contains(input_array[2]))
                {
                    firstname = input_array[0];
                    lastname = input_array[1];
                    suffix = input_array[2];
                }
                else if (input_array[1].Length == 1)
                {
                    firstname = input_array[0];
                    middlename = input_array[1];
                    lastname = input_array[2];
                }
                else if (surNames.Contains(input_array[2]))
                {
                    if (prefixes.Contains(input_array[0]))
                    {
                        prefix = input_array[0];
                        lastname = input_array[2];
                        firstname = input_array[1];
                    }
                    else if (prefixes.Contains(input_array[1]))
                    {
                        prefix = input_array[1];
                        lastname = input_array[2];
                        firstname = input_array[0];
                    }
                    else if (suffixes.Contains(input_array[0]))
                    {
                        firstname = input_array[1];
                        lastname = input_array[2];
                        suffix = input_array[0];
                    }
                    else if (suffixes.Contains(input_array[1]))
                    {
                        firstname = input_array[0];
                        lastname = input_array[2];
                        suffix = input_array[1];
                    }
                    else if (surNames.Contains(input_array[2]))
                    {
                        firstname = input_array[0];
                        middlename = input_array[1];
                        lastname = input_array[2];
                    }
                    else // Middle name
                    {
                    }
                }
                else if (surNames.Contains(input_array[1]))
                {
                    if (prefixes.Contains(input_array[0]))
                    {
                        prefix = input_array[0];
                        firstname = input_array[2];
                        lastname = input_array[1];
                    }
                    else if (prefixes.Contains(input_array[2]))
                    {
                        prefix = input_array[2];
                        firstname = input_array[0];
                        lastname = input_array[1];
                    }
                    else if (suffixes.Contains(input_array[0]))
                    {
                        suffix = input_array[0];
                        firstname = input_array[2];
                        lastname = input_array[1];
                    }
                    else if (suffixes.Contains(input_array[2]))
                    {
                        suffix = input_array[2];
                        firstname = input_array[0];
                        lastname = input_array[1];
                    }
                    else // Middle name
                    {
                        if (!hasComma)
                        {
                            firstname = input_array[0];
                            middlename = input_array[1];
                            lastname = input_array[2];
                        }
                    }
                }
                else if (surNames.Contains(input_array[0]))
                {
                    if (prefixes.Contains(input_array[1]))
                    {
                        prefix = input_array[1];
                        firstname = input_array[2];
                        lastname = input_array[0];
                    }
                    else if (prefixes.Contains(input_array[2]))
                    {
                        prefix = input_array[2];
                        firstname = input_array[1];
                        lastname = input_array[0];
                    }
                    else if (suffixes.Contains(input_array[1]))
                    {
                        suffix = input_array[1];
                        firstname = input_array[2];
                        lastname = input_array[0];
                    }
                    else if (suffixes.Contains(input_array[2]))
                    {
                        suffix = input_array[2];
                        firstname = input_array[1];
                        lastname = input_array[0];
                    }
                    else // Middle name
                    {
                        if (!hasComma)
                        {
                            firstname = input_array[0];
                            middlename = input_array[1];
                            lastname = input_array[2];
                        }
                    }
                }
            }
        }

        private void parseFourTokenName()
        {
            if (input_array[1].Length == 1)
            {
                if (prefixes.Contains(input_array[0]))
                {// P F M L
                    prefix = input_array[0];
                    firstname = input_array[1];
                    middlename = input_array[2];
                    lastname = input_array[3];
                }
                else if (input_array[2].Length == 1)
                { // F M M L
                    firstname = input_array[0];
                    middlename = input_array[1] + " " + input_array[2];
                    lastname = input_array[3];
                }
                else
                {// F M L S
                    firstname = input_array[0];
                    middlename = input_array[1];
                    lastname = input_array[2];
                    suffix = input_array[3];
                }
            }
            else if (input_array[2].Length == 1)
            {
                if (prefixes.Contains(input_array[0]))
                {// P F M L
                    prefix = input_array[0];
                    firstname = input_array[1];
                    middlename = input_array[2];
                    lastname = input_array[3];
                }
            }
            else if (prefixes.Contains(input_array[0]))
            {
                if (suffixes.Contains(input_array[3]))
                {
                    prefix = input_array[0];
                    firstname = input_array[1];
                    lastname = input_array[2];
                    suffix = input_array[3];
                }
                else if (prefixes.Contains(input_array[1]))
                {
                    prefix = input_array[0] + " " + input_array[1];
                    firstname = input_array[2];
                    lastname = input_array[3];
                }
                else if (maleNames.Contains(input_array[1]) || femaleNames.Contains(input_array[1]))
                {// P F M L
                    prefix = input_array[0];
                    firstname = input_array[1];
                    middlename = input_array[2];
                    lastname = input_array[3];
                }
            }
            else if (suffixes.Contains(input_array[3]))
            {
                if (suffixes.Contains(input_array[2]))
                {// F L S S
                    firstname = input_array[0];
                    lastname = input_array[1];
                    suffix = input_array[2] + " " + input_array[3];
                }
                //else if (maleNames.Contains(input_array[0]) || femaleNames.Contains(input_array[0]))
                //{// F M L S
                //}
                else
                {// F M L S
                    firstname = input_array[0];
                    middlename = input_array[1];
                    lastname = input_array[2];
                    suffix = input_array[3];
                }
            }
            else if (maleNames.Contains(input_array[0]) || femaleNames.Contains(input_array[0]))
            {
                if (surNames.Contains(input_array[3]))
                {//F M M L
                    firstname = input_array[0];
                    middlename = input_array[1] + " " + input_array[2];
                    lastname = input_array[3];
                }
                else
                {// F M L S
                    firstname = input_array[0];
                    middlename = input_array[1];
                    lastname = input_array[2];
                    suffix = input_array[3];
                }
            }
        }

        private void parseFiveTokenName()
        {
            if (prefixes.Contains(input_array[0]))
            {
                if (input_array[2].Length == 1)
                {
                    prefix = input_array[0];
                    firstname = input_array[1];
                    middlename = input_array[2];
                    lastname = input_array[3];
                    suffix = input_array[4];
                }
                else if (maleNames.Contains(input_array[1]) || (femaleNames.Contains(input_array[1])))
                {
                    if (surNames.Contains(input_array[2]))
                    {
                        prefix = input_array[0];
                        firstname = input_array[1];
                        lastname = input_array[2];
                        suffix = input_array[3] + ", " + input_array[4];
                    }
                    else if (surNames.Contains(input_array[3]))
                    {
                        prefix = input_array[0];
                        firstname = input_array[1];
                        middlename = input_array[2];
                        lastname = input_array[3];
                        suffix = input_array[4];
                    }
                }
                else if (suffixes.Contains(input_array[4]))
                {
                    if (prefixes.Contains(input_array[1]))
                    { // P P F L S
                        prefix = input_array[0] + " " + input_array[1];
                        firstname = input_array[2];
                        lastname = input_array[3];
                        suffix = input_array[4];
                    }
                    else if (suffixes.Contains(input_array[3]))
                    { // P F L S S
                        prefix = input_array[0];
                        firstname = input_array[1];
                        lastname = input_array[2];
                        suffix = input_array[3] + " " + input_array[4];
                    }
                    else
                    { // P F M L S
                        prefix = input_array[0];
                        firstname = input_array[1];
                        middlename = input_array[2];
                        lastname = input_array[3];
                        suffix = input_array[4];
                    }
                }
            }
            else if (suffixes.Contains(input_array[2]) && !prefixes.Contains(input_array[0]))
            {
                firstname = input_array[0];
                lastname = input_array[1];
                suffix = input_array[2] + ", " + input_array[3] + ", " + input_array[4];
            }
            else if (maleNames.Contains(input_array[0]) || femaleNames.Contains(input_array[0]))
            {
                if (input_array[1].Length == 1 && input_array[2].Length > 1)
                {
                    firstname = input_array[0];
                    middlename = input_array[1];
                    lastname = input_array[2];
                    suffix = input_array[3] + " " + input_array[4];
                }
            }
            if (firstname == "" && lastname == "")
            {
                riskyParse();
            }
        }

        private void parseSixTokenName()
        {
            riskyParse();
        }

        private void parseGreaterThanSixTokens()
        {
            riskyParse();
        }

        private void riskyParse()
        {
            int position = 0;
            foreach (string i in input_array)
            {
                if (prefixes.Contains(i) || suffixes.Contains(i))
                {
                    if (position == 0) { prefix = prefix + " " + i; }
                    else if (position == input_array.Length - 1) { suffix = suffix + " " + i; }
                    else
                    {
                        if (prefixes.Contains(i)) { prefix = prefix + " " + i; }
                        else if (suffixes.Contains(i)) { suffix = suffix + " " + i; }
                    }
                }
                else if (maleNames.Contains(i) || femaleNames.Contains(i))
                {
                    if (surNames.Contains(i))
                    { // could be first or last name
                        if (firstname == "") { firstname = i; }
                        else
                        {
                            if (lastname == "")
                            {
                                lastname = i;
                            }
                            else
                            {
                                middlename = middlename + " " + i;
                            }
                        }
                    }
                    else { firstname = i; }
                }
                else if (surNames.Contains(i))
                {
                    lastname = lastname + " " + i;
                }
                else if (i.Length == 1)
                {// initial
                    if (length == 2) { firstname = firstname + " " + i; }
                    if ((length == 3) && (position == 1)) { middlename = middlename + " " + i; }
                }
                else
                {
                    // not identified as first, last, suffix, or gender
                    if (Regex.IsMatch(i, @"\d")) { prefix = prefix + " " + i; }
                    else if (firstname != "") { lastname = lastname + " " + i; }
                }
                position++;
            }
            if (prefix == "" && firstname == "" && middlename == "" && lastname == "" && suffix == "")
            {
                if (length == 1)
                {
                }
                else if (length == 2)
                {
                    firstname = input_array[0];
                    lastname = input_array[1];
                }
                else if (length == 3)
                {
                    if (hasComma)
                    {
                        firstname = input_array[0];
                        lastname = input_array[1];
                        suffix = input_array[2];
                    }
                    else
                    {
                        firstname = input_array[0];
                        middlename = input_array[1];
                        lastname = input_array[2];
                    }
                }
            }
        }

        public string caseName(string name)
        {

            if (name.Length == 1)
            {
                return name.ToUpper() + '.'; // Add a period to the end.  
            }
            string output;
            // For nornal name
            if (Regex.IsMatch(name, @"^[a-zA-Z]+$"))
            {
                output = name.First().ToString().ToUpper() + name.Substring(1).ToLower();
            }
            // Deal with hyphenated names
            else if (name.Contains('-'))
            {
                try
                {
                    name = name.First().ToString().ToUpper() + name.Substring(1).ToLower();
                    int indexOfHyphen = name.IndexOf('-');
                    output = name.Substring(0, indexOfHyphen + 1) + name.Substring(indexOfHyphen + 1).First().ToString().ToUpper() + name.Substring(indexOfHyphen + 2).ToLower();
                }
                catch //(Exception ex)
                { // The component would throw an error if there is more than one hyphen in name
                    output = name.First().ToString().ToUpper() + name.Substring(1).ToLower();
                    //Console.WriteLine(ex.Message);
                }
            }
            else
            {
                string ret = "";
                output = name;
                if (output.Contains(' '))
                {
                    string[] nameArray = output.Split(new char[0]);
                    foreach (string i in nameArray)
                    {
                        ret = ret + " " + caseName(i);
                    }
                }
                output = ret.Trim();
            }
            // Check for irish names like O'Connell, etc.
            string[] irishNames = { "OROURKE", 
                                    "OREILLY", 
                                    "OHALLERAN", 
                                    "OHALLORAN", 
                                    "ONEILL", 
                                    "ONEIL", 
                                    "OCONNOR", 
                                    "OBRIEN",
                                    "OLEARY",
                                    "OCONNELL",
                                    "ODONNELL",
                                    "OSULLIVAN",
                                    "OMALLEY",
                                    "OFARRELL",
                                    "OTOOLE",
                                    "OKEEFE",
                                    "OQUINN"};
            foreach (string i in irishNames)
            {
                if (output.ToUpper() == i)
                {
                    try
                    {
                        return output.First().ToString().ToUpper() + "'" + output.Substring(1).First().ToString().ToUpper() + output.Substring(2).ToLower();
                    }
                    catch
                    {
                        return output = name.First().ToString().ToUpper() + name.Substring(1).ToLower();
                    }
                }
            }
            if (output.Length > 4 && output.Substring(0, 2).ToUpper() == "MC")
            {
                output = "Mc" + output.Substring(2).First().ToString().ToUpper() + output.Substring(3).ToLower();
            }
            else if (output.Length > 5 && output.Substring(0, 3).ToUpper() == "MAC")
            {
                output = "Mac" + output.Substring(3).First().ToString().ToUpper() + output.Substring(4).ToLower();
            }
            if (output.ToUpper() == "LADONNA")
            {
                output = "LaDonna";
            }
            return output;
        }

        private void populatePrefixAndSuffixDictionaries()
        {
            int count;
            string[] suffixArray1 = { "JR",
                                      "SR",
                                      "2ND",
                                      "3RD",
                                      "C3RD",
                                      "ESQ",
                                      "PHD",
                                      "RET",
                                      "JD",
                                      "MD",
                                      "DO",
                                      "DC",
                                      "BS",
                                      "MS",
                                      "BA",
                                      "MA",
                                      "MBA",
                                      "ESQUIRE",
                                      "MPH",
                                      "EDD",
                                      "ED.D.",
                                      "DED",
                                      "D.ED.",
                                      "DRPH"
                                    };
            string[] suffixArray2 = { "Jr.",
                                      "Sr.",
                                      "2nd",
                                      "3rd",
                                      "C3rd",
                                      "Esq.",
                                      "PhD",
                                      "Ret.",
                                      "J.D.",
                                      "M.D.",
                                      "D.O.",
                                      "D.C.",
                                      "B.S.",
                                      "M.S.",
                                      "B.A.",
                                      "M.A.",
                                      "M.B.A.",
                                      "Esquire",
                                      "M.P.H",
                                      "Ed.D.",
                                      "Ed.D.",
                                      "D.Ed.",
                                      "D.Ed.",
                                      "Dr.P.H"
                                    };
            count = 0;
            foreach (string s in suffixArray1)
            {
                if (!(SuffixDictionary.ContainsKey(s)))
                {
                    SuffixDictionary.Add(suffixArray1[count], suffixArray2[count]);
                }
                count++;
            }

            // Prefixes
            string[] prefixArray1 = { "DR",
                                      "MR",
                                      "MRS",
                                      "MS",
                                      "MISS",
                                      "ADM",
                                      "AMB",
                                      "GEN",
                                      "CPT",
                                      "CAPT",
                                      "BR",
                                      "CHAN",
                                      "CMDR",
                                      "COL",
                                      "CPL",
                                      "FR",
                                      "GOV",
                                      "LT",
                                      "MAJ",
                                      "PRES",
                                      "PROF",
                                      "REP",
                                      "REV",
                                      "SGT",
                                      "JUDGE"
                                    };
            string[] prefixArray2 = { "Dr.",
                                      "Mr.",
                                      "Mrs.",
                                      "Ms.",
                                      "Miss",
                                      "Adm.",
                                      "Amb.",
                                      "Gen.",
                                      "Cpt.",
                                      "Capt.",
                                      "Br.",
                                      "Chan.",
                                      "Cmdr.",
                                      "Col.",
                                      "Cpl.",
                                      "Fr.",
                                      "Gov.",
                                      "Lt.",
                                      "Maj.",
                                      "Pres.",
                                      "Prof.",
                                      "Rep.",
                                      "Rev.",
                                      "Sgt.",
                                      "Judge"
                                    };
            count = 0;
            foreach (string p in prefixArray1)
            {
                if (!(PrefixDictionary.ContainsKey(p)))
                {
                    PrefixDictionary.Add(prefixArray1[count], prefixArray2[count]);
                }
                count++;
            }

        }

        private string casePrefixes(string input)
        {
            input = input.Trim().ToUpper();
            foreach (var entry in PrefixDictionary)
            {
                if (input == entry.Key)
                {
                    input = entry.Value;
                }
            }
            return input;
        }

        private string caseSuffixes(string input)
        {
            input = input.Trim().ToUpper();
            foreach (var entry in SuffixDictionary)
            {
                if (input == entry.Key)
                {
                    input = entry.Value;
                }
            }
            return input;
        }

        private void guessGender(string fname)
        {
            if (maleNames.Contains(fname) && femaleNames.Contains(fname))
            {
                gender = "Unknown";
            }
            else if (maleNames.Contains(fname))
            {
                gender = "Male";
            }
            else if (femaleNames.Contains(fname))
            {
                gender = "Female";
            }
            else
            {
                gender = "Unknown";
            }
        }
    }
}
