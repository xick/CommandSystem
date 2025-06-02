using System.Collections.Generic;

namespace SickDev.CommandSystem
{
	//TODO CommandParser?
	//TODO maybe abstract or interface?
	public class ParsedCommand
	{
		const char separator = ' ';
		static readonly char[] groupifiers = { '\'', '\"' };

		public string raw { get; private set; }
		public string command { get; private set; }
		public ParsedArgument[] args { get; private set; }

		public ParsedCommand(string raw)
		{
			this.raw = raw;
			GetCommand();
			GetArgs();
		}

		void GetCommand()
		{
			string[] parts = raw.Split(separator);
			command = parts[0];
		}

		void GetArgs()
		{
            string stringArgs = raw.Substring(command.Length).Trim();
            List<string> listArgs = new List<string>();

            char? groupifier = null;
            string arg = string.Empty;
            foreach (char c in stringArgs)
            {
                //Args are separated by the separator character IF AND ONLY IF outside a group
                if (c == separator && groupifier == null)
                {
                    if (!string.IsNullOrEmpty(arg))
                    {
                        listArgs.Add(arg);
                        arg = string.Empty;
                    }
                    continue;
                }

                if (IsGroupifier(c))  
                {
                    if (groupifier == null)  //Open group
                        groupifier = c;
                   
                    else if (groupifier == c)  //Close group
                    {
                        listArgs.Add(arg);
                        arg = string.Empty;
                        groupifier = null;
                    }
                    //Ignore nested groupifiers
                    else
                        arg += c;
                }
                else
                    arg += c;
            }

            if (arg != string.Empty)
                listArgs.Add(arg);
            args = listArgs.ConvertAll(x => new ParsedArgument(x)).ToArray();

        }

        bool IsGroupifier(char character)
        {
            for (int i = 0; i < groupifiers.Length; i++)
                if (character == groupifiers[i])
                    return true;
            return false;
        }

	}
}