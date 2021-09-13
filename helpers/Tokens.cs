using System;

namespace KeplerTokens
{
    public enum TokenType
    {
        BooleanOperator,
        OrOperator,
        ConstantValue,

        // variable things
        DeclareVariable, // context dependant! if DeclareVariable is FIRST token and it doesn't already exist, CREATE the variable. otherwise, access the variable
        StaticVariableType,
        StaticModifier,

        // header things
        DeclareHeader, // context dependant! MUST follow a StartHeader!
        StartHeader,
        EndHeader,


        NonToken, // inverter token: invert the result of the following tokens
        GenericAssign, // "is" action dependant on context

        // function things
        DeclareFunction, // context dependant! if DeclareFunction follows a StartFunction, create the function! otherwise, it MUST follow a CallFunction.
        StartFunction,
        EndFunction,
        AssignFunctionType,
        FunctionReturn,
        StartArguments,
        StartPositionalArguments,
        StartNonPositionalArguments,
        CallFunction,

        // conditional things
        StartConditional,
        EndConditional,

        // operations
        GenericAdd,
        GenericSubtract,
        GenericMultiply,
        GenericPower,
        GenericDivide,
        GenericOperation,
        GenericModulo,
        GenericEquality, // equality comparison (if type != type, throw TypeError)
        GenericLessThan,
        GenericGreaterThan,
        GenericLessThanEqual,
        GenericGreaterThanEqual,

        AssignNonPositionalArgument,
        PositionalArgument,
        PositionalArgumentAssignment,
        NonPositionalArgument,

        // conditions
        ConditionalIf,
        ConditionalElse,
        ConditionalElseIf,

        // type stuff
        StaticBoolean,
        StaticInt,
        StaticFloat,
        StaticUnsignedInt,
        StaticUnsignedFloat,
        StartStaticArray,
        EndStaticArray,
        StartStaticList,
        EndStaticList,

        StaticString,

        // linking things
        LinkFile,

        ConsolePrint, // print to console

        // looping things
        StartInterval,
        DeclareInterval,
        EndInterval,
        StartLoop,
        DeclareLoop,
        EndLoop,
        BreakOut,

        // signaling things
        EOP, // End of Program
        EOL, // End of Line

        StartAssertion,

        UNRECOGNIZED
    }
    public enum VariableType
    {
        Float,
        Int,
        uFloat,
        uInt,
        String,
        Function,
        List,
        Array,
        Boolean
    }
    public enum OperationType
    {
        Add,
        Subtract,
        Divide,
        Multiply,
        Power,
        Modulo,
        Equality,
        GreaterThan,
        GreaterThanEqual,
        LessThan,
        LessThanEqual,
        And,
        Or
    }
    public class Float
    {
        public VariableType type = VariableType.Float;
        private float value;
        public Float(float value)
        {
            this.value = value;
        }
    }
    public class uFloat
    {
        public VariableType type = VariableType.uFloat;
        private float value;
        public uFloat(float value)
        {
            this.value = Math.Abs(value);
        }
    }
    public class Int
    {
        public VariableType type = VariableType.Int;
        private int value;
        public Int(int value)
        {
            this.value = value;
        }
    }
    public class uInt
    {
        public VariableType type = VariableType.Int;
        private uint value;
        public uInt(uint value)
        {
            this.value = value;
        }
    }
}