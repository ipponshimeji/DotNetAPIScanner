using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

namespace DotNetAPIScanner {
	public abstract class Command {
		#region types

		public class TaskKinds {
			#region constants

			public const string Help = "help";

			#endregion
		}

		#endregion


		#region constants

		public const char OptionMarkerChar = '-';

		public const int GeneralErrorExitCode = -1;

		#endregion


		#region data

		private bool help = false;

		private bool debugMode = false;


		public bool Executing { get; private set; } = false;

		#endregion


		#region properties

		public bool Help {
			get {
				return this.help;
			}
			set {
				SetCommandArgumentProperty<bool>(ref this.help, value);
			}
		}

		public bool DebugMode {
			get {
				return this.debugMode;
			}
			set {
				SetCommandArgumentProperty<bool>(ref this.debugMode, value);
			}
		}

		#endregion


		#region methods

		public int Run(string[] args) {
			int exitCode = GeneralErrorExitCode;
			try {
				// parse arguments
				ParseArguments(args);

				// execute the command
				exitCode = Execute();
			} catch (Exception exception) {
				HandleError(exception);
			}

			return exitCode;
		}

		public int Execute() {
			// check state
			if (this.Executing) {
				throw new InvalidOperationException("The object is already executing the command.");
			}

			// execute the command
			int exitCode = GeneralErrorExitCode;
			this.Executing = true;
			try {
				Exception error = null;
				string taskKind = OnExecuting();
				try {
					exitCode = Execute(taskKind);
				} catch (Exception e) {
					error = e;
					throw;
				} finally {
					exitCode = OnExecuted(exitCode, error);
				}
			} finally {
				this.Executing = false;
			}

			return exitCode;
		}

		#endregion


		#region methods - for derived class

		protected void SetCommandArgumentProperty<T>(ref T prop, T value) {
			// check state
			if (this.Executing) {
				throw new InvalidOperationException("You can set this property when the object is not executing command.");
			}

			prop = value;
		}

		#endregion


		#region overridables

		protected virtual void ParseArguments(string[] args) {
			// check argument
			if (args == null) {
				throw new ArgumentNullException(nameof(args));
			}

			// parse each argument
			using (IEnumerator<string> argEnumerator = ((IEnumerable<string>)args).GetEnumerator()) {
				while (argEnumerator.MoveNext()) {
					ParseArgument(argEnumerator);
				}
			}
		}

		protected virtual void ParseArgument(IEnumerator<string> argEnumerator) {
			// check argument
			if (argEnumerator == null) {
				throw new ArgumentNullException(nameof(argEnumerator));
			}

			string arg = argEnumerator.Current;
			if (IsOption(arg)) {
				// option
				HandleOption(arg, argEnumerator);
			} else {
				// normal argument
				HandleNormalArgument(arg, argEnumerator);
			}
		}

		protected virtual bool IsOption(string arg) {
			// check argument
			Debug.Assert(arg != null);

			return (0 < arg.Length) && (arg[0] == OptionMarkerChar);
		}

		protected virtual void HandleOption(string arg, IEnumerator<string> argEnumerator) {
			// check argument
			Debug.Assert(arg != null);

			switch (arg) {
				case "--debug":
					this.DebugMode = true;
					break;
				case "-h":
				case "--help":
					this.Help = true;
					break;
				default:
					throw new ApplicationException($"Unrecognized option: {arg}");
			}
		}

		protected virtual string GetOptionValue(string optionName, IEnumerator<string> argEnumerator) {
			// check argument
			if (optionName == null) {
				throw new ArgumentNullException(nameof(optionName));
			}
			if (argEnumerator == null) {
				throw new ArgumentNullException(nameof(argEnumerator));
			}

			// get next argument
			if (argEnumerator.MoveNext() == false) {
				throw new ApplicationException($"No value is given for option '{optionName}'");
			}
			return argEnumerator.Current;
		}

		protected virtual void HandleNormalArgument(string arg, IEnumerator<string> argEnumerator) {
			throw new ApplicationException($"Unrecognized argument: {arg}");
		}

		protected virtual string OnExecuting() {
			return this.Help ? TaskKinds.Help : null;
		}

		protected virtual int Execute(string taskKind) {
			switch (taskKind) {
				case TaskKinds.Help:
					return ShowHelp();
				default:
					throw new NotSupportedException($"Unsupported task kind: {taskKind}");
			}
		}

		protected virtual int OnExecuted(int exitCode, Exception error) {
			return (error == null) ? exitCode : GeneralErrorExitCode;
		}

		protected virtual void HandleError(Exception error) {
			// check argument
			if (error == null) {
				return;
			}

			// write error message to the standard error, by default
			WriteErrorTo(Console.Error, error);
		}

		protected virtual void WriteErrorTo(TextWriter writer, Exception error) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}
			if (error == null) {
				throw new ArgumentNullException(nameof(error));
			}

			// write 
			string message = this.DebugMode ? error.ToString() : error.Message;
			writer.WriteLine(message);
		}

		protected virtual int ShowHelp() {
			// output help to the standard out, by default
			WriteHelpTo(Console.Out);
			return 0;
		}

		protected virtual void WriteHelpTo(TextWriter writer) {
		}

		protected abstract string GetCommandName();

		#endregion
	}
}
