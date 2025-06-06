﻿using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace SickDev.CommandSystem
{
	public class CommandsManager
	{
		public delegate void OnCommandModified(Command command);

		NotificationsHandler notificationsHandler;
		ReflectionFinder finder;
		ArgumentsParser parser;
		CommandAttributeLoader loader;
		object block = new object();
		List<Command> commands = new List<Command>();

		public Configuration configuration { get; private set; }

		public bool isAllDataLoaded => parser.isDataLoaded;

		public event OnCommandModified onCommandAdded;
		public event OnCommandModified onCommandRemoved;
		public event Action onCommandsLoaded;

		public event NotificationsHandler.OnExceptionThrown onExceptionThrown
		{
			add => notificationsHandler.onExceptionThrown += value;
			remove => notificationsHandler.onExceptionThrown -= value;
		}

		public event NotificationsHandler.OnMessageSent onMessageSent
		{
			add => notificationsHandler.onMessageSent += value;
			remove => notificationsHandler.onMessageSent -= value;
		}

		//TODO Dependency injection
		public CommandsManager(Configuration configuration)
		{
			this.configuration = configuration;
			notificationsHandler = new NotificationsHandler();

			//TODO reflection finder should give you both commands attribute and parser attribute. Then, pass that info to arguments parser
			finder = new ReflectionFinder(configuration, notificationsHandler);
			parser = new ArgumentsParser(finder, notificationsHandler, configuration.allowThreading);
			loader = new CommandAttributeLoader(finder, notificationsHandler);
		}

		public void LoadCommands()
		{
			if (!configuration.allowThreading)
			{
				LoadCommandsInternal();
				return;
			}
			Thread thread = new Thread(LoadCommandsInternal);
			thread.Start();
		}

		void LoadCommandsInternal()
		{
			Add(loader.GetCommands());
			onCommandsLoaded?.Invoke();
		}

		public void Add(Command[] commands)
		{
			for (int i = 0; i < commands.Length; i++)
				Add(commands[i]);
		}

		public void Add(Command command)
		{
			lock (block)
			{
				if (IsCommandAdded(command))
					return;
				commands.Add(command);
				onCommandAdded?.Invoke(command);
			}
		}

		public bool IsCommandAdded(Command command) => commands.Any(x => command.Equals(x));

		public void Remove(Command[] commands)
		{
			for (int i = 0; i < commands.Length; i++)
				Remove(commands[i]);
		}

		public void Remove(Command command) => RemoveInternal(x => command.Equals(x));

		void RemoveInternal(Predicate<Command> predicate)
		{
			for (int i = commands.Count - 1; i >= 0; i--)
			{
				if (!predicate(commands[i]))
					continue;
				Command removedCommand = commands[i];
				commands.RemoveAt(i);
				onCommandRemoved?.Invoke(removedCommand);
			}
		}

		public void RemoveOverloads(Command[] commands)
		{
			for (int i = 0; i < commands.Length; i++)
				RemoveOverloads(commands[i]);
		}

		public void RemoveOverloads(Command command) => RemoveInternal(x => command.IsOverloadOf(x));
		public bool IsCommandOverloadAdded(Command command) => commands.Any(x => command.IsOverloadOf(x));
		public Command[] GetCommands() => commands.ToArray();
		public object Execute(string text) => GetCommandExecuter(text).Execute();

		public CommandExecuter GetCommandExecuter(string text)
		{
			ParsedCommand parsedCommand = new ParsedCommand(text);
			return GetCommandExecuter(parsedCommand);
		}

		public CommandExecuter GetCommandExecuter(ParsedCommand parsedCommand) => new CommandExecuter(commands, parsedCommand, parser, notificationsHandler);
	}
}