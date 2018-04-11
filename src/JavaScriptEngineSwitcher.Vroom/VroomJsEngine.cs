﻿using System;
using System.Collections.Generic;
using System.Text;

using OriginalAssemblyLoader = VroomJs.AssemblyLoader;
using OriginalJsContext = VroomJs.JsContext;
using OriginalJsEngine = VroomJs.JsEngine;
using OriginalJsException = VroomJs.JsException;
using OriginalJsInteropException = VroomJs.JsInteropException;

using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.Core.Utilities;
using CoreStrings = JavaScriptEngineSwitcher.Core.Resources.Strings;

using JavaScriptEngineSwitcher.Vroom.Utilities;

namespace JavaScriptEngineSwitcher.Vroom
{
	/// <summary>
	/// Adapter for the Vroom JS engine (cross-platform bridge to the V8 JS engine)
	/// </summary>
	public sealed class VroomJsEngine : JsEngineBase
	{
		/// <summary>
		/// Name of JS engine
		/// </summary>
		public const string EngineName = "VroomJsEngine";

		/// <summary>
		/// Version of original JS engine
		/// </summary>
		private const string EngineVersion = "3.17.16.2";

		/// <summary>
		/// Vroom JS engine
		/// </summary>
		private OriginalJsEngine _jsEngine;

		/// <summary>
		/// JS context
		/// </summary>
		private OriginalJsContext _jsContext;

		/// <summary>
		/// Synchronizer of code execution
		/// </summary>
		private readonly object _executionSynchronizer = new object();

		/// <summary>
		/// List of host items
		/// </summary>
		private readonly Dictionary<string, object> _hostItems = new Dictionary<string, object>();

		/// <summary>
		/// Synchronizer of JS engine initialization
		/// </summary>
		private static readonly object _initializationSynchronizer = new object();

		/// <summary>
		/// Flag indicating whether the JS engine is initialized
		/// </summary>
		private static bool _initialized;

		/// <summary>
		/// Unique document name manager
		/// </summary>
		private readonly UniqueDocumentNameManager _documentNameManager =
			new UniqueDocumentNameManager(DefaultDocumentName);

		/// <summary>
		/// Gets a name of JS engine
		/// </summary>
		public override string Name
		{
			get { return EngineName; }
		}

		/// <summary>
		/// Gets a version of original JS engine
		/// </summary>
		public override string Version
		{
			get { return EngineVersion; }
		}

		/// <summary>
		/// Gets a value that indicates if the JS engine supports garbage collection
		/// </summary>
		public override bool SupportsGarbageCollection
		{
			get { return false; }
		}


		/// <summary>
		/// Constructs an instance of adapter for the Vroom JS engine
		/// (cross-platform bridge to the V8 JS engine)
		/// </summary>
		public VroomJsEngine()
			: this(new VroomSettings())
		{ }

		/// <summary>
		/// Constructs an instance of adapter for the Vroom JS engine
		/// (cross-platform bridge to the V8 JS engine)
		/// </summary>
		/// <param name="settings">Settings of the Vroom JS engine</param>
		public VroomJsEngine(VroomSettings settings)
		{
			Initialize();

			VroomSettings vroomSettings = settings ?? new VroomSettings();

			try
			{
				_jsEngine = new OriginalJsEngine(vroomSettings.MaxYoungSpaceSize, vroomSettings.MaxOldSpaceSize);
				_jsContext = _jsEngine.CreateContext();
			}
			catch (Exception e)
			{
				throw new JsEngineLoadException(
					string.Format(CoreStrings.Runtime_JsEngineNotLoaded, EngineName, e.Message),
					EngineName, EngineVersion, e);
			}
			finally
			{
				if (_jsContext == null)
				{
					Dispose();
				}
			}
		}


		/// <summary>
		/// Initializes a JS engine
		/// </summary>
		private static void Initialize()
		{
			if (_initialized)
			{
				return;
			}

			lock (_initializationSynchronizer)
			{
				if (_initialized)
				{
					return;
				}

				if (Utils.IsWindows())
				{
					try
					{
						OriginalAssemblyLoader.EnsureLoaded();
					}
					catch (Exception e)
					{
						throw new JsEngineLoadException(
							string.Format(CoreStrings.Runtime_JsEngineNotLoaded, EngineName, e.Message),
							EngineName, EngineVersion, e);
					}
				}

				_initialized = true;
			}
		}

		/// <summary>
		/// Makes a mapping from the host type to a Vroom type
		/// </summary>
		/// <param name="value">The source value</param>
		/// <returns>The mapped value</returns>
		private static object MapToVroomType(object value)
		{
			if (value is Undefined)
			{
				return null;
			}

			return value;
		}

		private static JsRuntimeException ConvertJsExceptionToJsRuntimeException(
			OriginalJsException jsException)
		{
			string message = jsException.Message;
			string category;
			int lineNumber = 0;
			int columnNumber = 0;
			string sourceFragment = string.Empty;

			if (jsException is OriginalJsInteropException)
			{
				category = "InteropError";
			}
			else
			{
				category = jsException.Type;
				lineNumber = jsException.Line;
				columnNumber = jsException.Column;
			}

			var jsRuntimeException = new JsRuntimeException(message, EngineName, EngineVersion,
				jsException)
			{
				Category = category,
				LineNumber = lineNumber,
				ColumnNumber = columnNumber,
				SourceFragment = sourceFragment
			};

			return jsRuntimeException;
		}

		#region JsEngineBase implementation

		protected override object InnerEvaluate(string expression)
		{
			return InnerEvaluate(expression, null);
		}

		protected override object InnerEvaluate(string expression, string documentName)
		{
			object result;
			string uniqueDocumentName = _documentNameManager.GetUniqueName(documentName);

			lock (_executionSynchronizer)
			{
				try
				{
					result = _jsContext.Execute(expression, uniqueDocumentName);
				}
				catch (OriginalJsException e)
				{
					throw ConvertJsExceptionToJsRuntimeException(e);
				}
			}

			return result;
		}

		protected override T InnerEvaluate<T>(string expression)
		{
			return InnerEvaluate<T>(expression, null);
		}

		protected override T InnerEvaluate<T>(string expression, string documentName)
		{
			object result = InnerEvaluate(expression, documentName);

			return TypeConverter.ConvertToType<T>(result);
		}

		protected override void InnerExecute(string code)
		{
			InnerExecute(code, null);
		}

		protected override void InnerExecute(string code, string documentName)
		{
			string uniqueDocumentName = _documentNameManager.GetUniqueName(documentName);

			lock (_executionSynchronizer)
			{
				try
				{
					_jsContext.Execute(code, uniqueDocumentName);
				}
				catch (OriginalJsException e)
				{
					throw ConvertJsExceptionToJsRuntimeException(e);
				}
			}
		}

		protected override object InnerCallFunction(string functionName, params object[] args)
		{
			string functionCallExpression;
			int argumentCount = args.Length;

			if (argumentCount > 0)
			{
				var functionCallBuilder = new StringBuilder();
				functionCallBuilder.Append(functionName);
				functionCallBuilder.Append("(");

				for (int argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
				{
					object value = args[argumentIndex];
					string serializedValue = SimplisticJsSerializer.Serialize(value);

					if (argumentIndex > 0)
					{
						functionCallBuilder.Append(", ");
					}
					functionCallBuilder.Append(serializedValue);
				}

				functionCallBuilder.Append(");");

				functionCallExpression = functionCallBuilder.ToString();
				functionCallBuilder.Clear();
			}
			else
			{
				functionCallExpression = string.Format("{0}();", functionName);
			}

			object result = Evaluate(functionCallExpression);

			return result;
		}

		protected override T InnerCallFunction<T>(string functionName, params object[] args)
		{
			object result = InnerCallFunction(functionName, args);

			return TypeConverter.ConvertToType<T>(result);
		}

		protected override bool InnerHasVariable(string variableName)
		{
			string expression = string.Format("(typeof {0} !== 'undefined');", variableName);
			var result = InnerEvaluate<bool>(expression);

			return result;
		}

		protected override object InnerGetVariableValue(string variableName)
		{
			object result;

			lock (_executionSynchronizer)
			{
				try
				{
					result = _jsContext.GetVariable(variableName);
				}
				catch (OriginalJsException e)
				{
					throw ConvertJsExceptionToJsRuntimeException(e);
				}
			}

			return result;
		}

		protected override T InnerGetVariableValue<T>(string variableName)
		{
			object result = InnerGetVariableValue(variableName);

			return TypeConverter.ConvertToType<T>(result);
		}

		protected override void InnerSetVariableValue(string variableName, object value)
		{
			object processedValue = MapToVroomType(value);

			lock (_executionSynchronizer)
			{
				try
				{
					_jsContext.SetVariable(variableName, processedValue);
				}
				catch (OriginalJsException e)
				{
					throw ConvertJsExceptionToJsRuntimeException(e);
				}
			}
		}

		protected override void InnerRemoveVariable(string variableName)
		{
			string expression = string.Format(@"if (typeof {0} !== 'undefined') {{
	{0} = undefined;
}}", variableName);

			lock (_executionSynchronizer)
			{
				try
				{
					_jsContext.Execute(expression);

					if (_hostItems.ContainsKey(variableName))
					{
						_hostItems.Remove(variableName);
					}
				}
				catch (OriginalJsException e)
				{
					throw ConvertJsExceptionToJsRuntimeException(e);
				}
			}
		}

		private void EmbedHostItem(string itemName, object value)
		{
			lock (_executionSynchronizer)
			{
				object oldValue = null;
				if (_hostItems.ContainsKey(itemName))
				{
					oldValue = _hostItems[itemName];
				}
				_hostItems[itemName] = value;

				try
				{
					var delegateValue = value as Delegate;
					if (delegateValue != null)
					{
						_jsContext.SetFunction(itemName, delegateValue);
					}
					else
					{
						_jsContext.SetVariable(itemName, value);
					}
				}
				catch (OriginalJsException e)
				{
					if (oldValue != null)
					{
						_hostItems[itemName] = oldValue;
					}
					else
					{
						_hostItems.Remove(itemName);
					}

					throw ConvertJsExceptionToJsRuntimeException(e);
				}
			}
		}

		protected override void InnerEmbedHostObject(string itemName, object value)
		{
			object processedValue = MapToVroomType(value);
			EmbedHostItem(itemName, processedValue);
		}

		protected override void InnerEmbedHostType(string itemName, Type type)
		{
			EmbedHostItem(itemName, type);
		}

		protected override void InnerCollectGarbage()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IDisposable implementation

		public override void Dispose()
		{
			if (_disposedFlag.Set())
			{
				lock (_executionSynchronizer)
				{
					if (_jsContext != null)
					{
						_jsContext.Dispose();
						_jsContext = null;
					}

					if (_jsEngine != null)
					{
						_jsEngine.Dispose();
						_jsEngine = null;
					}

					if (_hostItems != null)
					{
						_hostItems.Clear();
					}
				}
			}
		}

		#endregion
	}
}