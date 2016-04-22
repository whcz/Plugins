﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Ctor.Resources;
using IronPython.Runtime;
using IronPython.Runtime.Exceptions;
using Microsoft.Scripting;

namespace Ctor.Models.Scripting
{
    internal class PythonScriptRunner
    {
        private readonly IScriptEditor _editor;
        private readonly TextOutputStream _output;
        private readonly PythonScriptEngine _engine;
        private readonly Dictionary<int, Action> _funcs;
        private readonly TaskScheduler _oknaUIScheduler;

        internal PythonScriptRunner(IScriptEditor editor, TextOutputStream output, 
            PythonScriptEngine scriptEngine, TaskScheduler oknaUIScheduler)
        {
            _editor = editor;
            _output = output;
            _engine = scriptEngine;
            _oknaUIScheduler = oknaUIScheduler;

            _funcs = new Dictionary<int, Action>
            {
                { DebugStrategy.NO_ACTION, () => { } },
                { DebugStrategy.CONTINUE, () => _dbgContinue.Set() },
                { DebugStrategy.TB_CALL, TracebackCall },
                { DebugStrategy.TB_RETURN, TracebackReturn },
                { DebugStrategy.TB_LINE, TracebackLine }
            };
            _dispatcher = ((FrameworkElement)editor).Dispatcher;
        }

        internal void Compile()
        {
            
        }

        internal void Run()
        {
            _editor.BeginScriptExecMode();
            this.DebugInfo = Strings.Running;
            RunCore(null);
        }

        internal void Debug()
        {
            _debugStrategy = StepIntoStrategy.Instance;

            _tracebackAction = new Action<TraceBackFrame, string, object>(OnTraceback);
            _scriptFinished = false;

            _editor.BeginScriptExecMode();
            RunCore(OnTracebackReceived);
        }

        internal event EventHandler ScriptFinished;
        internal event EventHandler DebugInfoChanged;
        internal event EventHandler<TracebackStepEventArgs> TracebackStep;

        private void RunCore(TracebackDelegate traceback)
        {
            string script = _editor.Text;

            Task task = Task.Factory
                .StartNew(() => RunCore(script, traceback), CancellationToken.None, TaskCreationOptions.None, _oknaUIScheduler)
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        this.LastException = t.Exception;
                    }
                    this.NotifyScriptFinished();
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        internal Exception LastException { get; private set; }

        private string _debugInfo;
        internal string DebugInfo
        {
            get { return _debugInfo; }
            private set
            {
                _debugInfo = value;
                var handler = this.DebugInfoChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        private void RunCore(string script, TracebackDelegate traceback)
        {
            _engine.SetOutput(_output);
            _engine.SetTrace(traceback);

            try
            {
                if (!_engine.Execute(script))
                {
                    _output.WriteLine(_engine.ErrorMessage);
                }
            }
            catch (ModelException me)
            {
            // TODO: modelexception
                MessageBox.Show(me.Message);
            }
            catch (CompilationException ex)
            {
                var inner = ex.InnerException;
                _output.WriteLine(inner.GetType().ToString() + ":");
                var syntaxError = inner as SyntaxErrorException;
                if (syntaxError != null)
                {
                    _output.WriteLine(string.Format(Strings.LineColumnInfo, syntaxError.Line, syntaxError.Column));
                }
                _output.WriteLine(inner.Message);
            }
            finally
            {
                if (_engine != null)
                {
                    _engine.SetTrace(null);
                }
            }
        }

        private void NotifyScriptFinished()
        {
            var handler = this.ScriptFinished;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }

            this.DebugInfo = Strings.Completed;
            _editor.EndScriptExecMode();
        }

        #region Methods for debuging

        private Dispatcher _dispatcher;
        AutoResetEvent _dbgContinue = new AutoResetEvent(false);
        Action<TraceBackFrame, string, object> _tracebackAction;
        TracebackDelegate _traceback;
        TraceBackFrame _curFrame;
        FunctionCode _curCode;

        DebugStrategy _debugStrategy;
        bool _scriptFinished;

        internal void StepInto()
        {
            if (_scriptFinished) return;

            _traceback = this.OnTracebackReceived;
            _debugStrategy = StepIntoStrategy.Instance;

            ExecuteStep();
        }

        internal void RunToEnd()
        {
            if (_scriptFinished) return;

            _editor.HighlightLine(null, HighlightType.None);
            _debugStrategy = RunToEndStrategy.Instance;

            ExecuteStep();
        }

        internal void StepOut()
        {
            if (_scriptFinished) return;

            _traceback = this.OnTracebackReceived;

            _debugStrategy = StepOutStrategy.Instance;
            StepOutStrategy.Instance.ResetReturnLevel();

            ExecuteStep();
        }

        internal void StepOver()
        {
            if (_scriptFinished) return;

            _traceback = this.OnTracebackReceived;
            _debugStrategy = StepOverStrategy.Instance;
            StepOverStrategy.Instance.ResetLevel();

            ExecuteStep();
        }

        internal void RunToBreakpoint()
        {
            if (_scriptFinished) return;

            _traceback = this.OnTracebackReceived;
            _debugStrategy = new RunToBreakPointStrategy(_editor);

            ExecuteStep();
        }

        private void ExecuteStep()
        {
            this.DebugInfo = Strings.Running;
            _dbgContinue.Set();
        }

        private TracebackDelegate OnTracebackReceived(TraceBackFrame frame, string result, object payload)
        {
            if (_debugStrategy.BreakTrace || string.Compare(result, "exception", StringComparison.InvariantCulture) == 0)
            {
                _dispatcher.BeginInvoke(_tracebackAction, frame, result, payload);
                _dbgContinue.WaitOne();
            }

            return _traceback;
        }

        private void OnTraceback(TraceBackFrame frame, string result, object payload)
        {
            // For call and line, payload is null.
            // For return, payload is the value being returned from the function. 
            // For exception, the payload is information about the exception and where it was thrown.

            _curFrame = frame;
            _curCode = frame.f_code;

            switch (result)
            {
                case "call":
                    RaiseTracebackStepEvent(TracebackStepType.Call, frame, payload);
                    _funcs[_debugStrategy.Call(_curFrame, _curCode)]();
                    break;

                case "line":
                    RaiseTracebackStepEvent(TracebackStepType.Line, frame, payload);
                    _funcs[_debugStrategy.Line(_curFrame, _curCode)]();
                    break;

                case "return":
                    RaiseTracebackStepEvent(TracebackStepType.Return, frame, payload);
                    _funcs[_debugStrategy.Return(_curFrame, _curCode)]();
                    break;

                case "exception":
                    RaiseTracebackStepEvent(TracebackStepType.Exception, frame, payload);
                    TracebackException();
                    break;

                default:
                    throw new NotSupportedException(string.Format("{0} not supported!", result));
            }
        }

        private void RaiseTracebackStepEvent(TracebackStepType stepType, TraceBackFrame frame, object payload)
        {
            var handler = TracebackStep;
            if (handler != null)
            {
                var locals = frame.f_locals as PythonDictionary;
                var globals = frame.f_globals as PythonDictionary;

                handler(this, new TracebackStepEventArgs(globals, locals, stepType, payload));
            }
        }

        private void TracebackCall()
        {
            this.DebugInfo = string.Format(Strings.TracebackCall, _curCode.co_name);
            _editor.HighlightLine((int)_curFrame.f_lineno, HighlightType.Call);
        }

        private void TracebackReturn()
        {
            this.DebugInfo = string.Format(Strings.TracebackReturn, _curCode.co_name);
            _editor.HighlightLine(_curCode.co_firstlineno, HighlightType.Return);
        }

        private void TracebackLine()
        {
            this.DebugInfo = string.Format(Strings.TracebackLine, _curFrame.f_lineno);
            _editor.HighlightLine((int)_curFrame.f_lineno, HighlightType.Line);
        }

        private void TracebackException()
        {
            _traceback = this.OnTracebackReceived;
            _debugStrategy = StepIntoStrategy.Instance;

            this.DebugInfo = string.Format(Strings.TracebackException, _curFrame.f_lineno);
            _editor.HighlightLine((int)_curFrame.f_lineno, HighlightType.Exception);
        }

        #endregion
    }
}
