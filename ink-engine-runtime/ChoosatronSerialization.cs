using System;
using System.Collections.Generic;
using System.Linq;

namespace Ink.Runtime
{
     public static class Choosatron
     {

        public static void WriteRuntimeContainer(SimpleChoosatron.Writer writer, Container container, bool withoutName = false)
        {
            //writer.WriteArrayStart();

            foreach (var c in container.content) {
                WriteRuntimeObject(writer, c);
            }

            // Container is always an array [...]
            // But the final element is always either:
            //  - a dictionary containing the named content, as well as possibly
            //    the key "#" with the count flags
            //  - null, if neither of the above
            // var namedOnlyContent = container.namedOnlyContent;
            // var countFlags = container.countFlags;
            // var hasNameProperty = container.name != null && !withoutName;

            // bool hasTerminator = namedOnlyContent != null || countFlags > 0 || hasNameProperty;

            // if( hasTerminator )
            //     writer.WriteObjectStart();

            // if ( namedOnlyContent != null ) {
            //     foreach(var namedContent in namedOnlyContent) {
            //         var name = namedContent.Key;
            //         var namedContainer = namedContent.Value as Container;
            //         writer.WritePropertyStart(name);
            //         WriteRuntimeContainer(writer, namedContainer, withoutName:true);
            //         writer.WritePropertyEnd();
            //     }
            // }

            // if (countFlags > 0)
            //     writer.WriteProperty("#f", countFlags);

            // if (hasNameProperty)
            //     writer.WriteProperty("#n", container.name);

            // if (hasTerminator)
            //     writer.WriteObjectEnd();
            // else
            //     writer.WriteNull();

            // writer.WriteArrayEnd();
        }

        public static void WriteRuntimeObject(SimpleChoosatron.Writer writer, Runtime.Object obj)
        {
            var container = obj as Container;
            if (container) {
                WriteRuntimeContainer(writer, container);
                return;
            }

            var divert = obj as Divert;
            if (divert)
            {
                string divTypeKey = "->";
                if (divert.isExternal)
                    divTypeKey = "x()";
                else if (divert.pushesToStack)
                {
                    if (divert.stackPushType == PushPopType.Function)
                        divTypeKey = "f()";
                    else if (divert.stackPushType == PushPopType.Tunnel)
                        divTypeKey = "->t->";
                }

                string targetStr;
                if (divert.hasVariableTarget)
                    targetStr = divert.variableDivertName;
                else
                    targetStr = divert.targetPathString;

                writer.WriteObjectStart();

                writer.WriteProperty(divTypeKey, targetStr);

                if (divert.hasVariableTarget)
                    writer.WriteProperty("var", true);

                if (divert.isConditional)
                    writer.WriteProperty("c", true);

                if (divert.externalArgs > 0)
                    writer.WriteProperty("exArgs", divert.externalArgs);

                writer.WriteObjectEnd();
                return;
            }

            var choicePoint = obj as ChoicePoint;
            if (choicePoint)
            {
                writer.WriteObjectStart();
                writer.WriteProperty("*", choicePoint.pathStringOnChoice);
                writer.WriteProperty("flg", choicePoint.flags);
                writer.WriteObjectEnd();
                return;
            }

            var boolVal = obj as BoolValue;
            if (boolVal) {
                writer.Write(boolVal.value);
                return;
            }

            var intVal = obj as IntValue;
            if (intVal) {
                writer.Write(intVal.value);
                return;
            }

            var floatVal = obj as FloatValue;
            if (floatVal) {
                writer.Write(floatVal.value);
                return;
            }

            var strVal = obj as StringValue;
            if (strVal)
            {
                if (strVal.isNewline)
                    writer.Write("\\n", escape:false);
                else {
                    writer.WriteStringStart();
                    writer.WriteStringInner("^");
                    writer.WriteStringInner(strVal.value);
                    writer.WriteStringEnd();
                }
                return;
            }

            var listVal = obj as ListValue;
            if (listVal)
            {
                // TODO
                //WriteInkList(writer, listVal);
                return;
            }

            var divTargetVal = obj as DivertTargetValue;
            if (divTargetVal)
            {
                writer.WriteObjectStart();
                writer.WriteProperty("^->", divTargetVal.value.componentsString);
                writer.WriteObjectEnd();
                return;
            }

            var varPtrVal = obj as VariablePointerValue;
            if (varPtrVal)
            {
                writer.WriteObjectStart();
                writer.WriteProperty("^var", varPtrVal.value);
                writer.WriteProperty("ci", varPtrVal.contextIndex);
                writer.WriteObjectEnd();
                return;
            }

            var glue = obj as Runtime.Glue;
            if (glue) {
                writer.Write("<>");
                return;
            }

            var controlCmd = obj as ControlCommand;
            if (controlCmd)
            {
                writer.Write(_controlCommandNames[(int)controlCmd.commandType]);
                return;
            }

            var nativeFunc = obj as Runtime.NativeFunctionCall;
            if (nativeFunc)
            {
                var name = nativeFunc.name;

                // Avoid collision with ^ used to indicate a string
                if (name == "^") name = "L^";

                writer.Write(name);
                return;
            }


            // Variable reference
            var varRef = obj as VariableReference;
            if (varRef)
            {
                writer.WriteObjectStart();

                string readCountPath = varRef.pathStringForCount;
                if (readCountPath != null)
                {
                    writer.WriteProperty("CNT?", readCountPath);
                }
                else
                {
                    writer.WriteProperty("VAR?", varRef.name);
                }

                writer.WriteObjectEnd();
                return;
            }

            // Variable assignment
            var varAss = obj as VariableAssignment;
            if (varAss)
            {
                writer.WriteObjectStart();

                string key = varAss.isGlobal ? "VAR=" : "temp=";
                writer.WriteProperty(key, varAss.variableName);

                // Reassignment?
                if (!varAss.isNewDeclaration)
                    writer.WriteProperty("re", true);

                writer.WriteObjectEnd();

                return;
            }

            // Void
            var voidObj = obj as Void;
            if (voidObj) {
                writer.Write("void");
                return;
            }

            // Tag
            var tag = obj as Tag;
            if (tag)
            {
                writer.WriteObjectStart();
                writer.WriteProperty("#", tag.text);
                Console.WriteLine( "#: " + tag.text);
                writer.WriteObjectEnd();
                return;
            }

            // Used when serialising save state only
            var choice = obj as Choice;
            if (choice) {
                WriteChoice(writer, choice);
                return;
            }

            throw new System.Exception("Failed to write runtime object to JSON: " + obj);
        }

        static Choice JObjectToChoice(Dictionary<string, object> jObj)
        {
            var choice = new Choice();
            choice.text = jObj ["text"].ToString();
            Console.Write("Choice: " + choice.text);
            choice.index = (int)jObj ["index"];
            choice.sourcePath = jObj ["originalChoicePath"].ToString();
            choice.originalThreadIndex = (int)jObj ["originalThreadIndex"];
            choice.pathStringOnChoice = jObj ["targetPath"].ToString();
            return choice;
        }

        public static void WriteChoice(SimpleChoosatron.Writer writer, Choice choice)
        {
            writer.WriteObjectStart();
            writer.WriteProperty("text", choice.text);
            writer.WriteProperty("index", choice.index);
            writer.WriteProperty("originalChoicePath", choice.sourcePath);
            writer.WriteProperty("originalThreadIndex", choice.originalThreadIndex);
            writer.WriteProperty("targetPath", choice.pathStringOnChoice);
            writer.WriteObjectEnd();
        }

        static void WriteInkList(SimpleChoosatron.Writer writer, ListValue listVal)
        {
            var rawList = listVal.value;

            writer.WriteObjectStart();

            writer.WritePropertyStart("list");

            writer.WriteObjectStart();

            foreach (var itemAndValue in rawList)
            {
                var item = itemAndValue.Key;
                int itemVal = itemAndValue.Value;

                writer.WritePropertyNameStart();
                writer.WritePropertyNameInner(item.originName ?? "?");
                writer.WritePropertyNameInner(".");
                writer.WritePropertyNameInner(item.itemName);
                writer.WritePropertyNameEnd();

                writer.Write(itemVal);

                writer.WritePropertyEnd();
            }

            writer.WriteObjectEnd();

            writer.WritePropertyEnd();

            if (rawList.Count == 0 && rawList.originNames != null && rawList.originNames.Count > 0)
            {
                writer.WritePropertyStart("origins");
                writer.WriteArrayStart();
                foreach (var name in rawList.originNames)
                    writer.Write(name);
                writer.WriteArrayEnd();
                writer.WritePropertyEnd();
            }

            writer.WriteObjectEnd();
        }

         static Choosatron() 
         {
            _controlCommandNames = new string[(int)ControlCommand.CommandType.TOTAL_VALUES];

            _controlCommandNames [(int)ControlCommand.CommandType.EvalStart] = "ev";
            _controlCommandNames [(int)ControlCommand.CommandType.EvalOutput] = "out";
            _controlCommandNames [(int)ControlCommand.CommandType.EvalEnd] = "/ev";
            _controlCommandNames [(int)ControlCommand.CommandType.Duplicate] = "du";
            _controlCommandNames [(int)ControlCommand.CommandType.PopEvaluatedValue] = "pop";
            _controlCommandNames [(int)ControlCommand.CommandType.PopFunction] = "~ret";
            _controlCommandNames [(int)ControlCommand.CommandType.PopTunnel] = "->->";
            _controlCommandNames [(int)ControlCommand.CommandType.BeginString] = "str";
            _controlCommandNames [(int)ControlCommand.CommandType.EndString] = "/str";
            _controlCommandNames [(int)ControlCommand.CommandType.NoOp] = "nop";
            _controlCommandNames [(int)ControlCommand.CommandType.ChoiceCount] = "choiceCnt";
            _controlCommandNames [(int)ControlCommand.CommandType.Turns] = "turn";
            _controlCommandNames [(int)ControlCommand.CommandType.TurnsSince] = "turns";
            _controlCommandNames [(int)ControlCommand.CommandType.ReadCount] = "readc";
            _controlCommandNames [(int)ControlCommand.CommandType.Random] = "rnd";
            _controlCommandNames [(int)ControlCommand.CommandType.SeedRandom] = "srnd";
            _controlCommandNames [(int)ControlCommand.CommandType.VisitIndex] = "visit";
            _controlCommandNames [(int)ControlCommand.CommandType.SequenceShuffleIndex] = "seq";
            _controlCommandNames [(int)ControlCommand.CommandType.StartThread] = "thread";
            _controlCommandNames [(int)ControlCommand.CommandType.Done] = "done";
            _controlCommandNames [(int)ControlCommand.CommandType.End] = "end";
            _controlCommandNames [(int)ControlCommand.CommandType.ListFromInt] = "listInt";
            _controlCommandNames [(int)ControlCommand.CommandType.ListRange] = "range";
            _controlCommandNames [(int)ControlCommand.CommandType.ListRandom] = "lrnd";

            for (int i = 0; i < (int)ControlCommand.CommandType.TOTAL_VALUES; ++i) {
                  if (_controlCommandNames [i] == null)
                     throw new System.Exception ("Control command not accounted for in serialisation");
            }
         }

         static string[] _controlCommandNames;

     }
}