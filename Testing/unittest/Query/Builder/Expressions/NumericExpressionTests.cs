/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2013 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using NUnit.Framework;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Arithmetic;
using VDS.RDF.Query.Expressions.Comparison;
using VDS.RDF.Query.Expressions.Primary;

namespace VDS.RDF.Query.Builder.Expressions
{
    [TestFixture]
    public class NumericExpressionTests : SparqlExpressionTestsBase
    {
        [SetUp]
        public void Setup()
        {
            Left = 10.ToConstantTerm();
            Right = 15.ToConstantTerm();
        }

        [Test]
        public void ShouldAllowComparingGenericAndNongenericNumericExpressions()
        {
            var left = new NumericExpression(Left);
            var right = new NumericExpression<int>(15);
            Right = right.Expression;

            AssertExpressionTypeAndCorrectArguments<EqualsExpression>(left == right);
            AssertExpressionTypeAndCorrectArguments<GreaterThanExpression>(left > right);
            AssertExpressionTypeAndCorrectArguments<GreaterThanOrEqualToExpression>(left >= right);
            AssertExpressionTypeAndCorrectArguments<LessThanExpression>(left < right);
            AssertExpressionTypeAndCorrectArguments<LessThanOrEqualToExpression>(left <= right);
            AssertExpressionTypeAndCorrectArguments<NotEqualsExpression>(left != right);
        }

        [Test]
        public void ShouldAllowComparingGenericAndNongenericNumericExpressionsReversed()
        {
            var right = new NumericExpression(Right);
            var left = new NumericExpression<int>(15);
            Left = left.Expression;

            AssertExpressionTypeAndCorrectArguments<EqualsExpression>(left == right);
            AssertExpressionTypeAndCorrectArguments<GreaterThanExpression>(left > right);
            AssertExpressionTypeAndCorrectArguments<GreaterThanOrEqualToExpression>(left >= right);
            AssertExpressionTypeAndCorrectArguments<LessThanExpression>(left < right);
            AssertExpressionTypeAndCorrectArguments<LessThanOrEqualToExpression>(left <= right);
            AssertExpressionTypeAndCorrectArguments<NotEqualsExpression>(left != right);
        }

        [Test]
        public void ShouldAllowArithmeticOperatorsWithGenericAndNongenericNumericExpressions()
        {
            NumericExpression left = new NumericExpression(Left);
            NumericExpression right = new NumericExpression<int>(15);
            Right = right.Expression;

            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * right);
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / right);
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + right);
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - right);
        }

        [Test]
        public void ShouldAllowArithmeticOperatorsWithGenericAndNongenericNumericExpressionsReversed()
        {
            NumericExpression left = new NumericExpression(Left);
            NumericExpression right = new NumericExpression<int>(15);
            Right = right.Expression;

            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * right);
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / right);
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + right);
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - right);
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithNumericExpressionAndVariableExpression()
        {
            // given
            NumericExpression left = new NumericExpression(Left);
            VariableExpression right = new VariableExpression("number");
            Right = right.Expression;

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * right);
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / right);
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + right);
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - right);
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithVariableExpressionAndNumericExpression()
        {
            // given
            NumericExpression right = new NumericExpression(Right);
            VariableExpression left = new VariableExpression("number");
            Left = left.Expression;

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * right);
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / right);
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + right);
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - right);
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithNumericExpressionAndInteger()
        {
            // given
            const int operandValue = 10;
            NumericExpression right = new NumericExpression(Right);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(operandValue * right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(operandValue / right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(operandValue + right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(operandValue - right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithIntegerAndNumericExpression()
        {
            // given
            const int operandValue = 10;
            NumericExpression left = new NumericExpression(Left);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithNumericExpressionAndLongInteger()
        {
            // given
            const long operandValue = 10;
            NumericExpression right = new NumericExpression(Right);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(operandValue * right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(operandValue / right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(operandValue + right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(operandValue - right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithLongIntegerAndNumericExpression()
        {
            // given
            const long operandValue = 10;
            NumericExpression left = new NumericExpression(Left);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithNumericExpressionAndShortInteger()
        {
            // given
            const short operandValue = 10;
            NumericExpression right = new NumericExpression(Right);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(operandValue * right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(operandValue / right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(operandValue + right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(operandValue - right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithShortIntegerAndNumericExpression()
        {
            // given
            const short operandValue = 10;
            NumericExpression left = new NumericExpression(Left);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithNumericExpressionAndDecimal()
        {
            // given
            const decimal operandValue = 10.5m;
            NumericExpression right = new NumericExpression(Right);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(operandValue * right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(operandValue / right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(operandValue + right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(operandValue - right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithDecimalAndNumericExpression()
        {
            // given
            const decimal operandValue = 10.5m;
            NumericExpression left = new NumericExpression(Left);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithNumericExpressionAndDouble()
        {
            // given
            const double operandValue = 10.5d;
            NumericExpression right = new NumericExpression(Right);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(operandValue * right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(operandValue / right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(operandValue + right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(operandValue - right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithDoubleAndNumericExpression()
        {
            // given
            const double operandValue = 10.5d;
            NumericExpression left = new NumericExpression(Left);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithNumericExpressionAndFloat()
        {
            // given
            const float operandValue = 10.5f;
            NumericExpression right = new NumericExpression(Right);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(operandValue * right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(operandValue / right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(operandValue + right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(operandValue - right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithFloatAndNumericExpression()
        {
            // given
            const float operandValue = 10.5f;
            NumericExpression left = new NumericExpression(Left);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithNumericExpressionAndByte()
        {
            // given
            const byte operandValue = 10;
            NumericExpression right = new NumericExpression(Right);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(operandValue * right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(operandValue / right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(operandValue + right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(operandValue - right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithByteAndNumericExpression()
        {
            // given
            const byte operandValue = 10;
            NumericExpression left = new NumericExpression(Left);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithNumericExpressionAndSignedByte()
        {
            // given
            const sbyte operandValue = 10;
            NumericExpression right = new NumericExpression(Right);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(operandValue * right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(operandValue / right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(operandValue + right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(operandValue - right,
                assertLeftOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }

        [Test]
        public void ShouldAllowUsingArithmeticOperatorsWithSignedByteAndNumericExpression()
        {
            // given
            const sbyte operandValue = 10;
            NumericExpression left = new NumericExpression(Left);

            // then
            AssertExpressionTypeAndCorrectArguments<MultiplicationExpression>(left * operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<DivisionExpression>(left / operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<AdditionExpression>(left + operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
            AssertExpressionTypeAndCorrectArguments<SubtractionExpression>(left - operandValue,
                assertRightOperand: ex => AssertCorrectConstantTerm(ex, operandValue));
        }
    }
}