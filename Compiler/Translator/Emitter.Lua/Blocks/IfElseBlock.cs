﻿using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace Bridge.Translator.Lua
{
    public class IfElseBlock : AbstractEmitterBlock
    {
        public IfElseBlock(IEmitter emitter, IfElseStatement ifElseStatement)
            : base(emitter, ifElseStatement)
        {
            this.Emitter = emitter;
            this.IfElseStatement = ifElseStatement;
        }

        public IfElseStatement IfElseStatement
        {
            get;
            set;
        }

        public List<IAsyncStep> EmittedAsyncSteps
        {
            get;
            set;
        }

        protected override void DoEmit()
        {
            this.VisitIfElseStatement();
        }

        protected void VisitIfElseStatement() {
            IfElseStatement ifElseStatement = this.IfElseStatement;

            this.WriteAwaiters(ifElseStatement.Condition);

            this.WriteIf();

            var oldValue = this.Emitter.ReplaceAwaiterByVar;
            this.Emitter.ReplaceAwaiterByVar = true;
            ifElseStatement.Condition.AcceptVisitor(this.Emitter);
            this.Emitter.ReplaceAwaiterByVar = oldValue;

            this.WriteSpace();
            this.WriteThen();

            int startCount = 0;
            int elseCount = 0;
            IAsyncStep trueStep = null;
            IAsyncStep elseStep = null;

            if(this.Emitter.IsAsync) {
                startCount = this.Emitter.AsyncBlock.Steps.Count;

                this.EmittedAsyncSteps = this.Emitter.AsyncBlock.EmittedAsyncSteps;
                this.Emitter.AsyncBlock.EmittedAsyncSteps = new List<IAsyncStep>();

                this.Emitter.IgnoreBlock = ifElseStatement.TrueStatement;
                this.WriteSpace();
                this.BeginBlock();
                this.Write("$step = " + this.Emitter.AsyncBlock.Step + ";");
                this.WriteNewLine();
                this.Write("continue;");
                var writer = this.SaveWriter();
                var bodyStep = this.Emitter.AsyncBlock.AddAsyncStep();
                ifElseStatement.TrueStatement.AcceptVisitor(this.Emitter);

                if(this.Emitter.AsyncBlock.Steps.Count > startCount) {
                    trueStep = this.Emitter.AsyncBlock.Steps.Last();
                }

                if(this.RestoreWriter(writer) && !this.IsOnlyWhitespaceOnPenultimateLine(true)) {
                    this.WriteNewLine();
                }

                this.EndBlock();
                this.WriteSpace();

                elseCount = this.Emitter.AsyncBlock.Steps.Count;
            }
            else {
                this.WriteNewLine();
                this.Indent();
                ifElseStatement.TrueStatement.AcceptVisitor(this.Emitter);
                this.Outdent();
            }

            if(ifElseStatement.FalseStatement != null && !ifElseStatement.FalseStatement.IsNull) {
                this.WriteElse();
                if(this.Emitter.IsAsync) {
                    this.Emitter.IgnoreBlock = ifElseStatement.FalseStatement;
                    this.WriteSpace();
                    this.BeginBlock();
                    this.Write("$step = " + this.Emitter.AsyncBlock.Step + ";");
                    this.WriteNewLine();
                    this.Write("continue;");
                    var writer = this.SaveWriter();
                    var bodyStep = this.Emitter.AsyncBlock.AddAsyncStep();
                    ifElseStatement.FalseStatement.AcceptVisitor(this.Emitter);

                    if(this.Emitter.AsyncBlock.Steps.Count > elseCount) {
                        elseStep = this.Emitter.AsyncBlock.Steps.Last();
                    }

                    if(this.RestoreWriter(writer) && !this.IsOnlyWhitespaceOnPenultimateLine(true)) {
                        this.WriteNewLine();
                    }

                    this.EndBlock();
                    this.WriteSpace();
                }
                else {
                    bool isNextIf = ifElseStatement.FalseStatement.FirstChild is IfElseStatement;
                    if(!isNextIf) {
                        this.WriteNewLine();
                        this.Indent();
                    }
                    ifElseStatement.FalseStatement.AcceptVisitor(this.Emitter);
                    if(!isNextIf) {
                        this.Outdent();
                        this.Write("end");
                        this.WriteNewLine();
                    }
                }
            }
            else {
                this.Write("end");
                this.WriteNewLine();
            }

            if(this.Emitter.IsAsync && this.Emitter.AsyncBlock.Steps.Count > startCount) {
                if(this.Emitter.AsyncBlock.Steps.Count <= elseCount && !AbstractEmitterBlock.IsJumpStatementLast(this.Emitter.Output.ToString())) {
                    this.WriteNewLine();
                    this.Write("$step = " + this.Emitter.AsyncBlock.Step + ";");
                    this.WriteNewLine();
                    this.Write("continue;");
                }

                var nextStep = this.Emitter.AsyncBlock.AddAsyncStep();

                if(trueStep != null) {
                    trueStep.JumpToStep = nextStep.Step;
                }

                if(elseStep != null) {
                    elseStep.JumpToStep = nextStep.Step;
                }
            }
            else if(this.Emitter.IsAsync) {
                this.WriteNewLine();
            }

            if(this.Emitter.IsAsync) {
                this.Emitter.AsyncBlock.EmittedAsyncSteps = this.EmittedAsyncSteps;
            }
        }

        /*
        protected void VisitIfElseStatement() {
            IfElseStatement ifElseStatement = this.IfElseStatement;

            this.WriteAwaiters(ifElseStatement.Condition);

            this.WriteIf();

            var oldValue = this.Emitter.ReplaceAwaiterByVar;
            this.Emitter.ReplaceAwaiterByVar = true;
            ifElseStatement.Condition.AcceptVisitor(this.Emitter);
            this.Emitter.ReplaceAwaiterByVar = oldValue;

            this.WriteSpace();
            this.BeginIfBlock();

            int startCount = 0;
            int elseCount = 0;
            IAsyncStep trueStep = null;
            IAsyncStep elseStep = null;

            if(this.Emitter.IsAsync) {
                startCount = this.Emitter.AsyncBlock.Steps.Count;

                this.EmittedAsyncSteps = this.Emitter.AsyncBlock.EmittedAsyncSteps;
                this.Emitter.AsyncBlock.EmittedAsyncSteps = new List<IAsyncStep>();

                this.Emitter.IgnoreBlock = ifElseStatement.TrueStatement;
                this.WriteSpace();
                this.BeginBlock();
                this.Write("$step = " + this.Emitter.AsyncBlock.Step + ";");
                this.WriteNewLine();
                this.Write("continue;");
                var writer = this.SaveWriter();
                var bodyStep = this.Emitter.AsyncBlock.AddAsyncStep();
                ifElseStatement.TrueStatement.AcceptVisitor(this.Emitter);

                if(this.Emitter.AsyncBlock.Steps.Count > startCount) {
                    trueStep = this.Emitter.AsyncBlock.Steps.Last();
                }

                if(this.RestoreWriter(writer) && !this.IsOnlyWhitespaceOnPenultimateLine(true)) {
                    this.WriteNewLine();
                }

                this.EndBlock();
                this.WriteSpace();

                elseCount = this.Emitter.AsyncBlock.Steps.Count;
            }
            else {
                ifElseStatement.TrueStatement.AcceptVisitor(this.Emitter);
                this.Outdent();
            }

            if(ifElseStatement.FalseStatement != null && !ifElseStatement.FalseStatement.IsNull) {
                if(this.Emitter.IsAsync) {
                    this.WriteElse();

                    this.Emitter.IgnoreBlock = ifElseStatement.FalseStatement;
                    this.WriteSpace();
                    this.BeginBlock();
                    this.Write("$step = " + this.Emitter.AsyncBlock.Step + ";");
                    this.WriteNewLine();
                    this.Write("continue;");
                    var writer = this.SaveWriter();
                    var bodyStep = this.Emitter.AsyncBlock.AddAsyncStep();
                    ifElseStatement.FalseStatement.AcceptVisitor(this.Emitter);

                    if(this.Emitter.AsyncBlock.Steps.Count > elseCount) {
                        elseStep = this.Emitter.AsyncBlock.Steps.Last();
                    }

                    if(this.RestoreWriter(writer) && !this.IsOnlyWhitespaceOnPenultimateLine(true)) {
                        this.WriteNewLine();
                    }

                    this.EndBlock();
                    this.WriteSpace();
                }
                else {
                    if(ifElseStatement.FalseStatement.FirstChild.Role == Roles.LBrace) {
                        this.BeginElseBlock();
                    }
                    else {
                        this.WriteElse();
                    }
                    ifElseStatement.FalseStatement.AcceptVisitor(this.Emitter);
                }
            }
            else {
                this.Indent();
                this.EndCodeBlock();
                this.WriteNewLine();
            }

            if(this.Emitter.IsAsync && this.Emitter.AsyncBlock.Steps.Count > startCount) {
                if(this.Emitter.AsyncBlock.Steps.Count <= elseCount && !AbstractEmitterBlock.IsJumpStatementLast(this.Emitter.Output.ToString())) {
                    this.WriteNewLine();
                    this.Write("$step = " + this.Emitter.AsyncBlock.Step + ";");
                    this.WriteNewLine();
                    this.Write("continue;");
                }

                var nextStep = this.Emitter.AsyncBlock.AddAsyncStep();

                if(trueStep != null) {
                    trueStep.JumpToStep = nextStep.Step;
                }

                if(elseStep != null) {
                    elseStep.JumpToStep = nextStep.Step;
                }
            }
            else if(this.Emitter.IsAsync) {
                this.WriteNewLine();
            }

            if(this.Emitter.IsAsync) {
                this.Emitter.AsyncBlock.EmittedAsyncSteps = this.EmittedAsyncSteps;
            }
        }*/

        /*
        protected void VisitIfElseStatement()
        {
            IfElseStatement ifElseStatement = this.IfElseStatement;

            this.WriteAwaiters(ifElseStatement.Condition);

            this.WriteIf();
            //this.WriteOpenParentheses();

            var oldValue = this.Emitter.ReplaceAwaiterByVar;
            this.Emitter.ReplaceAwaiterByVar = true;
            ifElseStatement.Condition.AcceptVisitor(this.Emitter);
            this.Emitter.ReplaceAwaiterByVar = oldValue;

            //this.WriteCloseParentheses();

            int startCount = 0;
            int elseCount = 0;
            IAsyncStep trueStep = null;
            IAsyncStep elseStep = null;

            if (this.Emitter.IsAsync)
            {
                startCount = this.Emitter.AsyncBlock.Steps.Count;

                this.EmittedAsyncSteps = this.Emitter.AsyncBlock.EmittedAsyncSteps;
                this.Emitter.AsyncBlock.EmittedAsyncSteps = new List<IAsyncStep>();

                this.Emitter.IgnoreBlock = ifElseStatement.TrueStatement;
                this.WriteSpace();
                this.BeginBlock();
                this.Write("$step = " + this.Emitter.AsyncBlock.Step + ";");
                this.WriteNewLine();
                this.Write("continue;");
                var writer = this.SaveWriter();
                var bodyStep = this.Emitter.AsyncBlock.AddAsyncStep();
                ifElseStatement.TrueStatement.AcceptVisitor(this.Emitter);

                if (this.Emitter.AsyncBlock.Steps.Count > startCount)
                {
                    trueStep = this.Emitter.AsyncBlock.Steps.Last();
                }

                if (this.RestoreWriter(writer) && !this.IsOnlyWhitespaceOnPenultimateLine(true))
                {
                    this.WriteNewLine();
                }

                this.EndBlock();
                this.WriteSpace();

                elseCount = this.Emitter.AsyncBlock.Steps.Count;
            }
            else
            {
                this.EmitBlockOrIndentedLine(ifElseStatement.TrueStatement);
            }

            if (ifElseStatement.FalseStatement != null && !ifElseStatement.FalseStatement.IsNull)
            {
                this.WriteElse();
                if (this.Emitter.IsAsync)
                {
                    this.Emitter.IgnoreBlock = ifElseStatement.FalseStatement;
                    this.WriteSpace();
                    this.BeginBlock();
                    this.Write("$step = " + this.Emitter.AsyncBlock.Step + ";");
                    this.WriteNewLine();
                    this.Write("continue;");
                    var writer = this.SaveWriter();
                    var bodyStep = this.Emitter.AsyncBlock.AddAsyncStep();
                    ifElseStatement.FalseStatement.AcceptVisitor(this.Emitter);

                    if (this.Emitter.AsyncBlock.Steps.Count > elseCount)
                    {
                        elseStep = this.Emitter.AsyncBlock.Steps.Last();
                    }

                    if (this.RestoreWriter(writer) && !this.IsOnlyWhitespaceOnPenultimateLine(true))
                    {
                        this.WriteNewLine();
                    }

                    this.EndBlock();
                    this.WriteSpace();
                }
                else
                {
                    this.EmitBlockOrIndentedLine(ifElseStatement.FalseStatement);
                }
            }

            if (this.Emitter.IsAsync && this.Emitter.AsyncBlock.Steps.Count > startCount)
            {
                if (this.Emitter.AsyncBlock.Steps.Count <= elseCount && !AbstractEmitterBlock.IsJumpStatementLast(this.Emitter.Output.ToString()))
                {
                    this.WriteNewLine();
                    this.Write("$step = " + this.Emitter.AsyncBlock.Step + ";");
                    this.WriteNewLine();
                    this.Write("continue;");
                }

                var nextStep = this.Emitter.AsyncBlock.AddAsyncStep();

                if (trueStep != null)
                {
                    trueStep.JumpToStep = nextStep.Step;
                }

                if (elseStep != null)
                {
                    elseStep.JumpToStep = nextStep.Step;
                }
            }
            else if (this.Emitter.IsAsync)
            {
                this.WriteNewLine();
            }

            if (this.Emitter.IsAsync)
            {
                this.Emitter.AsyncBlock.EmittedAsyncSteps = this.EmittedAsyncSteps;
            }
        }*/
    }
}