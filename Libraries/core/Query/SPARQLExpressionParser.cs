﻿/*

Copyright Robert Vesse 2009-10
rvesse@vdesign-studios.com

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Contexts;
using VDS.RDF.Parsing.Tokens;
using VDS.RDF.Query.Aggregates;
using VDS.RDF.Query.Patterns;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Functions;

namespace VDS.RDF.Query
{
    /// <summary>
    /// Internal Class which parses SPARQL Expressions into Expression Trees
    /// </summary>
    class SparqlExpressionParser
    {
        private NamespaceMapper _nsmapper;
        private Uri _baseUri;
        private bool _allowAggregates = false;
        private SparqlQuerySyntax _syntax = Options.QueryDefaultSyntax;
        private SparqlQueryParser _parser;
        private IEnumerable<ISparqlCustomExpressionFactory> _factories = Enumerable.Empty<ISparqlCustomExpressionFactory>();

        /// <summary>
        /// Creates a new SPARQL Expression Parser
        /// </summary>
        public SparqlExpressionParser() { }

        /// <summary>
        /// Creates a new SPARQL Expression Parser which has a reference back to a Query Parser
        /// </summary>
        /// <param name="parser">Query Parser</param>
        public SparqlExpressionParser(SparqlQueryParser parser)
            : this(parser, false) { }

        /// <summary>
        /// Creates a new SPARQL Expression Parser
        /// </summary>
        /// <param name="allowAggregates">Whether Aggregates are allowed in Expressions</param>
        public SparqlExpressionParser(bool allowAggregates)
            : this(null, allowAggregates) { }

        /// <summary>
        /// Creates a new SPARQL Expression Parser which has a reference back to a Query Parser
        /// </summary>
        /// <param name="parser">Query Parser</param>
        /// <param name="allowAggregates">Whether Aggregates are allowed in Expressions</param>
        public SparqlExpressionParser(SparqlQueryParser parser, bool allowAggregates)
        {
            this._parser = parser;
            this._allowAggregates = allowAggregates;
        }

        /// <summary>
        /// Sets the Base Uri used to resolve URIs and QNames
        /// </summary>
        public Uri BaseUri
        {
            set
            {
                this._baseUri = value;
            }
        }

        /// <summary>
        /// Sets the Namespace Map used to resolve QNames
        /// </summary>
        public NamespaceMapper NamespaceMap
        {
            set
            {
                this._nsmapper = value;
            }
        }

        /// <summary>
        /// Gets/Sets whether Aggregates are permitted in Expressions
        /// </summary>
        public bool AllowAggregates
        {
            get
            {
                return this._allowAggregates;
            }
            set
            {
                this._allowAggregates = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Syntax that should be supported
        /// </summary>
        public SparqlQuerySyntax SyntaxMode
        {
            get
            {
                return this._syntax;
            }
            set
            {
                this._syntax = value;
            }
        }

        /// <summary>
        /// Sets the Query Parser that the Expression Parser can call back into when needed
        /// </summary>
        public SparqlQueryParser QueryParser
        {
            set
            {
                this._parser = value;
            }
        }

        /// <summary>
        /// Gets/Sets the locally scoped custom expression factories
        /// </summary>
        public IEnumerable<ISparqlCustomExpressionFactory> ExpressionFactories
        {
            get
            {
                return this._factories;
            }
            set
            {
                if (value != null)
                {
                    this._factories = value;
                }
            }
        }

        /// <summary>
        /// Parses a SPARQL Expression
        /// </summary>
        /// <param name="tokens">Tokens that the Expression should be parsed from</param>
        /// <returns></returns>
        public ISparqlExpression Parse(Queue<IToken> tokens)
        {
            try
            {
                return this.TryParseConditionalOrExpression(tokens);
            }
            catch (InvalidOperationException ex)
            {
                //The Queue was empty
                throw new RdfParseException("Unexpected end of Token Queue while trying to parse an Expression", ex);
            }
        }

        private ISparqlExpression TryParseConditionalOrExpression(Queue<IToken> tokens) {
            //Get the first Term in the Expression
            ISparqlExpression firstTerm = this.TryParseConditionalAndExpression(tokens);

            if (tokens.Count > 0) 
            {
                //Expect an || Token
                IToken next = tokens.Dequeue();
                if (next.TokenType == Token.OR) 
                {
                    return new OrExpression(firstTerm, this.TryParseConditionalOrExpression(tokens));
                } 
                else 
                {
                    throw Error("Unexpected Token '" + next.GetType().ToString() + "' encountered while trying to parse a Conditional Or expression", next);
                }
            } 
            else 
            {
                return firstTerm;
            }
        }

        private ISparqlExpression TryParseConditionalAndExpression(Queue<IToken> tokens)
        {
            //Get the first Term in the Expression
            ISparqlExpression firstTerm = this.TryParseValueLogical(tokens);

            if (tokens.Count > 0)
            {
                //Expect an && Token
                IToken next = tokens.Peek();
                if (next.TokenType == Token.AND)
                {
                    tokens.Dequeue();
                    return new AndExpression(firstTerm, this.TryParseConditionalAndExpression(tokens));
                }
                else
                {
                    return firstTerm;
                    //throw new RdfParseException("Unexpected Token '" + next.GetType().ToString() + "' encountered while trying to parse a Conditional And expression");
                }
            }
            else
            {
                return firstTerm;
            }
        }

        private ISparqlExpression TryParseValueLogical(Queue<IToken> tokens)
        {
            return this.TryParseRelationalExpression(tokens);
        }

        private ISparqlExpression TryParseRelationalExpression(Queue<IToken> tokens)
        {
            //Get the First Term of this Expression
            ISparqlExpression firstTerm = this.TryParseNumericExpression(tokens);

            if (tokens.Count > 0)
            {
                IToken next = tokens.Peek();
                switch (next.TokenType)
                {
                    case Token.EQUALS:
                        tokens.Dequeue();
                        return new EqualsExpression(firstTerm, this.TryParseNumericExpression(tokens));
                    case Token.NOTEQUALS:
                        tokens.Dequeue();
                        return new NotEqualsExpression(firstTerm, this.TryParseNumericExpression(tokens));
                    case Token.LESSTHAN:
                        tokens.Dequeue();
                        return new LessThanExpression(firstTerm, this.TryParseNumericExpression(tokens));
                    case Token.GREATERTHAN:
                        tokens.Dequeue();
                        return new GreaterThanExpression(firstTerm, this.TryParseNumericExpression(tokens));
                    case Token.LESSTHANOREQUALTO:
                        tokens.Dequeue();
                        return new LessThanOrEqualToExpression(firstTerm, this.TryParseNumericExpression(tokens));
                    case Token.GREATERTHANOREQUALTO:
                        tokens.Dequeue();
                        return new GreaterThanOrEqualToExpression(firstTerm, this.TryParseNumericExpression(tokens));
                    default:
                        return firstTerm;
                }
            }
            else
            {
                return firstTerm;
            }
        }

        private ISparqlExpression TryParseNumericExpression(Queue<IToken> tokens)
        {
            return this.TryParseAdditiveExpression(tokens);
        }

        private ISparqlExpression TryParseAdditiveExpression(Queue<IToken> tokens)
        {
            //Get the First Term of this Expression
            ISparqlExpression firstTerm = this.TryParseMultiplicativeExpression(tokens);

            if (tokens.Count > 0)
            {
                IToken next = tokens.Peek();
                switch (next.TokenType)
                {
                    case Token.PLUS:
                        tokens.Dequeue();
                        return new AdditionExpression(firstTerm, this.TryParseMultiplicativeExpression(tokens));
                    case Token.MINUS:
                        tokens.Dequeue();
                        return new SubtractionExpression(firstTerm, this.TryParseMultiplicativeExpression(tokens));
                    case Token.PLAINLITERAL:
                        tokens.Dequeue();
                        return new AdditionExpression(firstTerm, this.TryParseNumericLiteral(next,tokens));
                    default:
                        return firstTerm;
                }
            }
            else
            {
                return firstTerm;
            }
        }

        private ISparqlExpression TryParseMultiplicativeExpression(Queue<IToken> tokens)
        {
            //Get the First Term of this Expression
            ISparqlExpression firstTerm = this.TryParseUnaryExpression(tokens);

            if (tokens.Count > 0)
            {
                IToken next = tokens.Peek();
                switch (next.TokenType)
                {
                    case Token.MULTIPLY:
                        tokens.Dequeue();
                        return new MultiplicationExpression(firstTerm, this.TryParseUnaryExpression(tokens));
                    case Token.DIVIDE:
                        tokens.Dequeue();
                        return new DivisionExpression(firstTerm, this.TryParseUnaryExpression(tokens));
                    default:
                        return firstTerm;
                }
            }
            else
            {
                return firstTerm;
            }
        }

        private ISparqlExpression TryParseUnaryExpression(Queue<IToken> tokens)
        {
            IToken next = tokens.Peek();

            switch (next.TokenType)
            {
                case Token.NEGATION:
                    tokens.Dequeue();
                    return new NegationExpression(this.TryParsePrimaryExpression(tokens));
                case Token.PLUS:
                    //Semantically Unary Plus does nothing so no special expression class for it
                    tokens.Dequeue();
                    return this.TryParsePrimaryExpression(tokens);
                case Token.MINUS:
                    tokens.Dequeue();
                    return new MinusExpression(this.TryParsePrimaryExpression(tokens));
                default:
                    return this.TryParsePrimaryExpression(tokens);
            }
        }

        private ISparqlExpression TryParsePrimaryExpression(Queue<IToken> tokens)
        {
            IToken next = tokens.Peek();

            switch (next.TokenType)
            {
                case Token.LEFTBRACKET:
                    return this.TryParseBrackettedExpression(tokens);

                case Token.BOUND:
                case Token.COALESCE:
                case Token.DATATYPEFUNC:
                case Token.EXISTS:
                case Token.IF:
                case Token.IRI:
                case Token.ISBLANK:
                case Token.ISIRI:
                case Token.ISLITERAL:
                case Token.ISURI:
                case Token.LANG:
                case Token.LANGMATCHES:
                case Token.NOTEXISTS:
                case Token.SAMETERM:
                case Token.STR:
                case Token.STRDT:
                case Token.STRLANG:
                case Token.REGEX:
                case Token.URIFUNC:
                    return this.TryParseBuiltInCall(tokens);

                case Token.AVG:
                case Token.COUNT:
                case Token.GROUPCONCAT:
                case Token.MAX:
                case Token.MEDIAN:
                case Token.MIN:
                case Token.MODE:
                case Token.NMAX:
                case Token.NMIN:
                case Token.SAMPLE:
                case Token.SUM:
                    if (this._allowAggregates)
                    {
                        return this.TryParseAggregateExpression(tokens);
                    }
                    else
                    {
                        throw Error("Unexpected Token '" + next.GetType().ToString() + "' encountered, Aggregates are not permitted in this Expression", next);
                    }

                case Token.URI:
                case Token.QNAME:
                    return this.TryParseIriRefOrFunction(tokens);

                case Token.LITERAL:
                case Token.LONGLITERAL:
                    return this.TryParseRdfLiteral(tokens);
                    
                case Token.PLAINLITERAL:
                    return this.TryParseBooleanOrNumericLiteral(tokens);

                case Token.VARIABLE:
                    tokens.Dequeue();

                    if (tokens.Count > 0)
                    {
                        //If the Variable is followed by an IN/NOT IN then we'll parse the Set function
                        if (tokens.Peek().TokenType == Token.IN || tokens.Peek().TokenType == Token.NOTIN)
                        {
                            return this.TryParseSetExpression(new VariableExpressionTerm(next.Value), tokens);
                        }
                        else
                        {
                            return new VariableExpressionTerm(next.Value);
                        }
                    }
                    else
                    {
                        return new VariableExpressionTerm(next.Value);
                    }

                default:
                    throw Error("Unexpected Token '" + next.GetType().ToString() + "' encountered while trying to parse a Primary Expression",next);
            }
        }

        private ISparqlExpression TryParseBrackettedExpression(Queue<IToken> tokens)
        {
            return this.TryParseBrackettedExpression(tokens, true);
        }

        private ISparqlExpression TryParseBrackettedExpression(Queue<IToken> tokens, bool requireOpeningLeftBracket)
        {
            bool temp = false;
            return this.TryParseBrackettedExpression(tokens, requireOpeningLeftBracket, out temp);
        }

        private ISparqlExpression TryParseBrackettedExpression(Queue<IToken> tokens, bool requireOpeningLeftBracket, out bool commaTerminated)
        {
            IToken next;

            commaTerminated = false;

            //Discard the Opening Bracket
            if (requireOpeningLeftBracket)
            {
                next = tokens.Dequeue();
                if (next.TokenType != Token.LEFTBRACKET)
                {
                    throw Error("Unexpected Token '" + next.GetType().ToString() + "' encountered, expected a Left Bracket to start a Bracketted Expression",next);
                }
            }

            int openBrackets = 1;
            Queue<IToken> exprTerms = new Queue<IToken>();

            while (openBrackets > 0)
            {
                //Get next Token
                next = tokens.Peek();

                //Take account of nesting
                if (next.TokenType == Token.LEFTBRACKET)
                {
                    openBrackets++;
                }
                else if (next.TokenType == Token.RIGHTBRACKET)
                {
                    openBrackets--;
                }
                else if (next.TokenType == Token.COMMA && openBrackets == 1)
                {
                    openBrackets--;
                    commaTerminated = true;
                }
                else if (next.TokenType == Token.DISTINCT && openBrackets == 1)
                {
                    //DISTINCT can terminate the Tokens that make an expression if it occurs as the first thing and only 1 bracket is open
                    if (tokens.Count == 0)
                    {
                        tokens.Dequeue();
                        commaTerminated = true;
                        return new DistinctModifierExpression();
                    }
                    else
                    {
                        throw Error("Unexpected DISTINCT Keyword Token encountered, DISTINCT modifier keyword may only occur as the first argument to an aggregate function", next);
                    }
                }

                if (openBrackets > 0)
                {
                    exprTerms.Enqueue(next);
                }
                tokens.Dequeue();
            }

            if (exprTerms.Count > 0)
            {
                //Recurse to invoke self
                return this.Parse(exprTerms);
            }
            else
            {
                return null;
            }
        }

        private ISparqlExpression TryParseBuiltInCall(Queue<IToken> tokens)
        {
            IToken next = tokens.Dequeue();

            switch (next.TokenType)
            {
                case Token.BOUND:
                    //Expect a Left Bracket, Variable and then a Right Bracket
                    next = tokens.Dequeue();
                    if (next.TokenType == Token.LEFTBRACKET)
                    {
                        next = tokens.Dequeue();
                        if (next.TokenType == Token.VARIABLE)
                        {
                            VariableExpressionTerm varExpr = new VariableExpressionTerm(next.Value);
                            next = tokens.Dequeue();
                            if (next.TokenType == Token.RIGHTBRACKET)
                            {
                                return new BoundFunction(varExpr);
                            }
                            else
                            {
                                throw Error("Unexpected Token '" + next.GetType().ToString() + "' encountered, a Right Bracket to end a BOUND function call was expected",next);
                            }
                        }
                        else
                        {
                            throw Error("Unexpected Token '" + next.GetType().ToString() + "' encountered, a Variable Token for a BOUND function call was expected", next);
                        }
                    }
                    else
                    {
                        throw Error("Unexpected Token '" + next.GetType().ToString() + "' encountered, a Left Bracket to start a BOUND function call was expected", next);
                    }
                case Token.COALESCE:
                    //Get as many argument expressions as there are
                    List<ISparqlExpression> args = new List<ISparqlExpression>();
                    bool commaTerminated = false;
                    bool first = true;
                    do
                    {
                        args.Add(this.TryParseBrackettedExpression(tokens, first, out commaTerminated));
                        first = false;
                    } while (commaTerminated);

                    return new CoalesceFunction(args);

                case Token.DATATYPEFUNC:
                    return new DataTypeFunction(this.TryParseBrackettedExpression(tokens));
                case Token.IF:
                    bool comma;
                    return new IfElseFunction(this.TryParseBrackettedExpression(tokens, true, out comma), this.TryParseBrackettedExpression(tokens, false, out comma), this.TryParseBrackettedExpression(tokens, false, out comma));
                case Token.IRI:
                    return new IriFunction(this.TryParseBrackettedExpression(tokens));
                case Token.ISBLANK:
                    return new IsBlankFunction(this.TryParseBrackettedExpression(tokens));
                case Token.ISIRI:
                    return new IsIriFunction(this.TryParseBrackettedExpression(tokens));
                case Token.ISLITERAL:
                    return new IsLiteralFunction(this.TryParseBrackettedExpression(tokens));
                case Token.ISURI:
                    return new IsUriFunction(this.TryParseBrackettedExpression(tokens));
                case Token.LANG:
                    return new LangFunction(this.TryParseBrackettedExpression(tokens));
                case Token.LANGMATCHES:
                    return new LangMatchesFunction(this.TryParseBrackettedExpression(tokens), this.TryParseBrackettedExpression(tokens, false));
                case Token.SAMETERM:
                    return new SameTermFunction(this.TryParseBrackettedExpression(tokens), this.TryParseBrackettedExpression(tokens, false));
                case Token.STR:
                    return new StrFunction(this.TryParseBrackettedExpression(tokens));
                case Token.STRDT:
                    return new StrDtFunction(this.TryParseBrackettedExpression(tokens), this.TryParseBrackettedExpression(tokens, false));
                case Token.STRLANG:
                    return new StrLangFunction(this.TryParseBrackettedExpression(tokens), this.TryParseBrackettedExpression(tokens, false));
                case Token.REGEX:
                    return this.TryParseRegexExpression(tokens);
                case Token.URIFUNC:
                    return new IriFunction(this.TryParseBrackettedExpression(tokens));

                case Token.EXISTS:
                case Token.NOTEXISTS:
                    if (this._syntax == SparqlQuerySyntax.Sparql_1_0) throw new RdfParseException("EXISTS/NOT EXISTS clauses are not supported in SPARQL 1.0");
                    if (this._parser == null) throw new RdfParseException("Unable to parse an EXISTS/NOT EXISTS as there is no Query Parser to call into");

                    //Gather Tokens for the Pattern
                    NonTokenisedTokenQueue temptokens = new NonTokenisedTokenQueue();
                    int openbrackets = 0;
                    bool mustExist = (next.TokenType == Token.EXISTS);
                    do
                    {
                        if (tokens.Count == 0) throw new RdfParseException("Unexpected end of Tokens while trying to parse an EXISTS/NOT EXISTS function");

                        next = tokens.Dequeue();
                        if (next.TokenType == Token.LEFTCURLYBRACKET)
                        {
                            openbrackets++;
                        }
                        else if (next.TokenType == Token.RIGHTCURLYBRACKET)
                        {
                            openbrackets--;
                        }
                        temptokens.Enqueue(next);
                    } while (openbrackets > 0);

                    //Call back into the Query Parser to try and Parse the Graph Pattern for the Function
                    SparqlQueryParserContext tempcontext = new SparqlQueryParserContext(temptokens);
                    tempcontext.Query.NamespaceMap.Import(this._nsmapper);
                    tempcontext.Query.BaseUri = this._baseUri;
                    return new ExistsFunction(this._parser.TryParseGraphPattern(tempcontext, true), mustExist);

                default:
                    throw Error("Unexpected Token '" + next.GetType().ToString() + "' encountered while trying to parse a Built-in Function call", next);
            }
        }

        private ISparqlExpression TryParseRegexExpression(Queue<IToken> tokens)
        {
            bool hasOptions = false;

            //Get Text and Pattern Expressions
            ISparqlExpression textExpr = this.TryParseBrackettedExpression(tokens);
            ISparqlExpression patternExpr = this.TryParseBrackettedExpression(tokens, false, out hasOptions);

            //Check whether we need to get an Options Expression
            if (hasOptions)
            {
                ISparqlExpression optionExpr = this.TryParseBrackettedExpression(tokens, false);
                return new RegexFunction(textExpr, patternExpr, optionExpr);
            }
            else
            {
                return new RegexFunction(textExpr, patternExpr);
            }
        }

        private ISparqlExpression TryParseIriRefOrFunction(Queue<IToken> tokens)
        {
            //Get the Uri/QName Token
            IToken first = tokens.Dequeue();

            //Resolve the Uri
            Uri u;
            if (first.TokenType == Token.QNAME)
            {
                //Resolve QName
                u = new Uri(Tools.ResolveQName(first.Value, this._nsmapper, this._baseUri));
            }
            else
            {
                String baseUri = (this._baseUri == null) ? String.Empty : this._baseUri.ToString();
                u = new Uri(Tools.ResolveUri(first.Value,baseUri));
            }
            
            //Get the Argument List (if any)
            if (tokens.Count > 0)
            {
                IToken next = tokens.Peek();
                if (next.TokenType == Token.LEFTBRACKET)
                {
                    bool comma = false;
                    List<ISparqlExpression> args = new List<ISparqlExpression>();
                    args.Add(this.TryParseBrackettedExpression(tokens, true, out comma));

                    while (comma)
                    {
                        args.Add(this.TryParseBrackettedExpression(tokens, false, out comma));
                    }

                    //If there are no arguments (one null argument) then discard
                    if (args.Count == 1 && args.First() == null) args.Clear();

                    //Return an Extension Function expression
                    ISparqlExpression expr = SparqlExpressionFactory.CreateExpression(u, args, this._factories);
                    if (expr is AggregateExpressionTerm || expr is NonNumericAggregateExpressionTerm)
                    {
                        if (!this._allowAggregates) throw new RdfParseException("Aggregate Expression '" + expr.ToString() + "' encountered but aggregates are not permitted in this Expression");
                    }
                    return expr;
                }
                else
                {
                    //Just an IRIRef
                    return new NodeExpressionTerm(new UriNode(null, u));
                }
            }
            else
            {
                //Just an IRIRef
                return new NodeExpressionTerm(new UriNode(null, u));
            }
        }

        private ISparqlExpression TryParseRdfLiteral(Queue<IToken> tokens)
        {
            //First Token will be the String value of this RDF Literal
            IToken str = tokens.Dequeue();

            //Might have a Language Specifier/DataType afterwards
            if (tokens.Count > 0)
            {
                IToken next = tokens.Peek();
                if (next.TokenType == Token.LANGSPEC)
                {
                    tokens.Dequeue();
                    return new NodeExpressionTerm(new LiteralNode(null, str.Value, next.Value));
                }
                else if (next.TokenType == Token.HATHAT)
                {
                    tokens.Dequeue();

                    //Should be a DataTypeToken afterwards
                    next = tokens.Dequeue();
                    LiteralWithDataTypeToken dtlit = new LiteralWithDataTypeToken(str, (DataTypeToken)next); ;
                    Uri u;

                    if (next.Value.StartsWith("<"))
                    {
                        u = new Uri(next.Value.Substring(1, next.Value.Length - 2));
                    }
                    else
                    {
                        //Resolve the QName
                        u = new Uri(Tools.ResolveQName(next.Value, this._nsmapper, this._baseUri));
                    }

                    if (SparqlSpecsHelper.GetNumericTypeFromDataTypeUri(u) != SparqlNumericType.NaN)
                    {
                        //Should be a Number
                        return this.TryParseNumericLiteral(dtlit, tokens);
                    }
                    else if (XmlSpecsHelper.XmlSchemaDataTypeBoolean.Equals(u.ToString()))
                    {
                        //Appears to be a Boolean
                        return new BooleanExpressionTerm(Boolean.Parse(dtlit.Value));
                    }
                    else
                    {
                        //Just a datatyped Literal Node
                        return new NodeExpressionTerm(new LiteralNode(null, str.Value, u));
                    }
                }
                else
                {
                    return new NodeExpressionTerm(new LiteralNode(null, str.Value));
                }
            }
            else
            {
                return new NodeExpressionTerm(new LiteralNode(null, str.Value));
            }

        }

        private ISparqlExpression TryParseBooleanOrNumericLiteral(Queue<IToken> tokens)
        {
            //First Token must be a Plain Literal
            IToken lit = tokens.Dequeue();

            if (lit.Value.Equals("true"))
            {
                return new BooleanExpressionTerm(true);
            }
            else if (lit.Value.Equals("false"))
            {
                return new BooleanExpressionTerm(false);
            }
            else
            {
                return this.TryParseNumericLiteral(lit, tokens);
            }
        }

        private ISparqlExpression TryParseNumericLiteral(IToken literal, Queue<IToken> tokens)
        {
            switch (literal.TokenType)
            {
                case Token.PLAINLITERAL:
                    //Use Regular Expressions to see what type it is
                    if (SparqlSpecsHelper.IsInteger(literal.Value))
                    {
                        return new NumericExpressionTerm(Int32.Parse(literal.Value));
                    }
                    else if (SparqlSpecsHelper.IsDecimal(literal.Value))
                    {
                        return new NumericExpressionTerm(Decimal.Parse(literal.Value));
                    }
                    else if (SparqlSpecsHelper.IsDouble(literal.Value))
                    {
                        return new NumericExpressionTerm(Double.Parse(literal.Value));
                    }
                    else
                    {
                        throw Error("The Plain Literal '" + literal.Value + "' is not a valid Integer, Decimal or Double", literal);
                    }
                    
                case Token.LITERALWITHDT:
                    //Get the Data Type Uri
                    String dt = ((LiteralWithDataTypeToken)literal).DataType;
                    String dtUri;
                    if (dt.StartsWith("<"))
                    {
                        String baseUri = (this._baseUri == null) ? String.Empty : this._baseUri.ToString();
                        dtUri = Tools.ResolveUri(dt.Substring(1, dt.Length - 2), baseUri);
                    }
                    else
                    {
                        dtUri = Tools.ResolveQName(dt, this._nsmapper, this._baseUri);
                    }

                    //Return a Numeric Expression Term if it's an Integer/Decimal/Double
                    if (XmlSpecsHelper.XmlSchemaDataTypeInteger.Equals(dtUri) && SparqlSpecsHelper.IsInteger(literal.Value))
                    {
                        return new NumericExpressionTerm(Int32.Parse(literal.Value));
                    }
                    else if (XmlSpecsHelper.XmlSchemaDataTypeDecimal.Equals(dtUri) && SparqlSpecsHelper.IsDecimal(literal.Value))
                    {
                        return new NumericExpressionTerm(Decimal.Parse(literal.Value));
                    }
                    else if (XmlSpecsHelper.XmlSchemaDataTypeFloat.Equals(dtUri) && SparqlSpecsHelper.IsFloat(literal.Value))
                    {
                        return new NumericExpressionTerm(Single.Parse(literal.Value));
                    }
                    else if (XmlSpecsHelper.XmlSchemaDataTypeDouble.Equals(dtUri) && SparqlSpecsHelper.IsDouble(literal.Value))
                    {
                        return new NumericExpressionTerm(Double.Parse(literal.Value));
                    }
                    else
                    {
                        throw Error("The Literal '" + literal.Value + "' with Datatype URI '" + dtUri + "' is not a valid Integer, Decimal or Double", literal);
                    }
                    
                case Token.LITERAL:
                    //Check if there's a Datatype following the Literal
                    if (tokens.Count > 0)
                    {
                        IToken next = tokens.Peek();
                        if (next.TokenType == Token.HATHAT)
                        {
                            tokens.Dequeue();
                            //Should now see a DataTypeToken
                            DataTypeToken datatype = (DataTypeToken)tokens.Dequeue();
                            LiteralWithDataTypeToken dtlit = new LiteralWithDataTypeToken(literal, datatype);

                            //Self-recurse to save replicating code
                            return this.TryParseNumericLiteral(dtlit, tokens);
                        }
                        else
                        {
                            //Use Regex to see if it's a Integer/Decimal/Double
                            if (SparqlSpecsHelper.IsInteger(literal.Value))
                            {
                                return new NumericExpressionTerm(Int32.Parse(literal.Value));
                            }
                            else if (SparqlSpecsHelper.IsDecimal(literal.Value))
                            {
                                return new NumericExpressionTerm(Decimal.Parse(literal.Value));
                            }
                            else if (SparqlSpecsHelper.IsDouble(literal.Value))
                            {
                                return new NumericExpressionTerm(Double.Parse(literal.Value));
                            }
                            else
                            {
                                //Otherwise treat as a Node Expression
                                throw Error("The Literal '" + literal.Value + "' is not a valid Integer, Decimal or Double", literal);
                            }
                        }
                    }
                    else
                    {
                        //Use Regular Expressions to see what type it is
                        if (SparqlSpecsHelper.IsInteger(literal.Value))
                        {
                            return new NumericExpressionTerm(Int32.Parse(literal.Value));
                        }
                        else if (SparqlSpecsHelper.IsDecimal(literal.Value))
                        {
                            return new NumericExpressionTerm(Decimal.Parse(literal.Value));
                        }
                        else if (SparqlSpecsHelper.IsDouble(literal.Value))
                        {
                            return new NumericExpressionTerm(Double.Parse(literal.Value));
                        }
                        else
                        {
                            throw Error("The Literal '" + literal.Value + "' is not a valid Integer, Decimal or Double", literal);
                        }
                    }

                default:
                    throw Error("Unexpected Token '" + literal.GetType().ToString() + "' encountered while trying to parse a Numeric Literal", literal);
            }
        }

        private ISparqlExpression TryParseAggregateExpression(Queue<IToken> tokens)
        {
            if (this._syntax == SparqlQuerySyntax.Sparql_1_0) throw new RdfParseException("Aggregates are not permitted in SPARQL 1.0");

            IToken agg = tokens.Dequeue();
            ISparqlExpression aggExpr = null, sepExpr = null;
            bool distinct = false, all = false;
            bool sepGroupConcat = false, fullGroupConcat = false;

            //Expect a Left Bracket next
            IToken next = tokens.Dequeue();
            if (next.TokenType != Token.LEFTBRACKET)
            {
                throw Error("Unexpected Token '" + next.GetType().ToString() + "', expected a Left Bracket after an Aggregate Keyword", next);
            }

            //Then a possible DISTINCT/ALL
            next = tokens.Dequeue();
            if (next.TokenType == Token.DISTINCT)
            {
                distinct = true;
                next = tokens.Dequeue();
            }
            if (next.TokenType == Token.ALL || next.TokenType == Token.MULTIPLY)
            {
                all = true;
                next = tokens.Dequeue();
            }

            //If we've seen an ALL then we need the closing bracket
            if (all && next.TokenType != Token.RIGHTBRACKET)
            {
                throw Error("Unexpected Token '" + next.GetType().ToString() + "', expected a Right Bracket after the * specifier in an aggregate to terminate the aggregate", next);
            }
            else if (all && agg.TokenType != Token.COUNT)
            {
                throw new RdfQueryException("Cannot use the * specifier in aggregates other than COUNT");
            }
            else if (!all)
            {
                //If it's not an all then we expect an expression
                //Gather the Tokens and parse the Expression
                Queue<IToken> subtokens = new Queue<IToken>();
                int openBrackets = 1;
                do
                {
                    if (next.TokenType == Token.LEFTBRACKET)
                    {
                        openBrackets++;
                    }
                    else if (next.TokenType == Token.RIGHTBRACKET)
                    {
                        openBrackets--;
                    }
                    else if (next.TokenType == Token.COMMA)
                    {
                        //If we see a comma when we only have 1 bracket open and this is a GROUP_CONCAT then this is a GROUP_CONCAT
                        //which concatenates multiple expressions
                        if (openBrackets == 1 && agg.TokenType == Token.GROUPCONCAT)
                        {
                            fullGroupConcat = true;
                            break;
                        }
                    }
                    else if (next.TokenType == Token.SEMICOLON)
                    {
                        //If we see a semicolon when we only have 1 bracket open and this is a GROUP_CONCAT then this is a GROUP_CONCAT
                        //which should have a separator
                        if (openBrackets == 1 && agg.TokenType == Token.GROUPCONCAT)
                        {
                            sepGroupConcat = true;
                            break;
                        }
                    }

                    if (openBrackets > 0)
                    {
                        subtokens.Enqueue(next);
                        next = tokens.Dequeue();
                    }
                } while (openBrackets > 0);

                aggExpr = this.Parse(subtokens);
            }

            //If we're dealing with a GROUP_CONCAT may have additional expressions to parse
            if (fullGroupConcat)
            {
                //Find each additional expression
                int openBrackets = 1;
                List<ISparqlExpression> expressions = new List<ISparqlExpression>();
                expressions.Add(aggExpr);

                while (openBrackets > 0)
                {
                    Queue<IToken> subtokens = new Queue<IToken>();
                    next = tokens.Dequeue();
                    do
                    {
                        if (next.TokenType == Token.LEFTBRACKET)
                        {
                            openBrackets++;
                        }
                        else if (next.TokenType == Token.RIGHTBRACKET)
                        {
                            openBrackets--;
                        }
                        else if (next.TokenType == Token.COMMA)
                        {
                            //If we see a comma when we only have 1 bracket open and this is a GROUP_CONCAT then this is a GROUP_CONCAT
                            //which concatenates multiple expressions
                            if (openBrackets == 1 && agg.TokenType == Token.GROUPCONCAT)
                            {
                                break;
                            }
                        }
                        else if (next.TokenType == Token.SEMICOLON)
                        {
                            //If we see a semicolon when we only have 1 bracket open and this is a GROUP_CONCAT then this is a GROUP_CONCAT
                            //which should have a separator
                            if (openBrackets == 1 && agg.TokenType == Token.GROUPCONCAT)
                            {
                                sepGroupConcat = true;
                                break;
                            }
                        }

                        if (openBrackets > 0)
                        {
                            subtokens.Enqueue(next);
                            next = tokens.Dequeue();
                        }
                    } while (openBrackets > 0);

                    //Parse this expression and add to the list of expressions we're concatenating
                    expressions.Add(this.Parse(subtokens));

                    //Once we've hit the ; for the scalar arguments then we can stop looking for expressions
                    if (sepGroupConcat) break;

                    //If we've hit a , then openBrackets will still be one and we'll go around again looking for another expression
                    //Otherwise we've reached the end of the GROUP_CONCAT and there was no ; for scalar arguments
                }
                // throw new NotSupportedException("GROUP_CONCAT over multiple expressions is not yet supported");

                //Represent the Expression we're concantenating as an XPath concat() function
                aggExpr = new XPathConcatFunction(expressions);
            }

            //Not that this is not an else, if we're doing a fullGroupConcat and we hit an ; then we stop as we expect a SEPARATOR
            //so we'll set sepGroupConcat to true causing us to drop in here and parse the separator
            if (sepGroupConcat)
            {
                //Need to parse SEPARATOR
                next = tokens.Peek();
                if (next.TokenType != Token.SEPARATOR)
                {
                    throw Error("Unexpected Token '" + next.GetType().ToString() + "' encountered, expected a SEPARATOR keyword as the argument for a GROUP_CONCAT aggregate", next);
                }
                tokens.Dequeue();

                //Then need an equals sign
                next = tokens.Peek();
                if (next.TokenType != Token.EQUALS)
                {
                    throw Error("Unexpected Token '" + next.GetType().ToString() + "' encountered, expected a = after a SEPARATOR keyword in a GROUP_CONCAT aggregate", next);
                }
                tokens.Dequeue();

                //Get the subtokens for the Separator Expression
                Queue<IToken> subtokens = new Queue<IToken>();
                int openBrackets = 1;
                next = tokens.Dequeue();
                do
                {
                    if (next.TokenType == Token.LEFTBRACKET)
                    {
                        openBrackets++;
                    }
                    else if (next.TokenType == Token.RIGHTBRACKET)
                    {
                        openBrackets--;
                    }
                    if (openBrackets > 0)
                    {
                        subtokens.Enqueue(next);
                        next = tokens.Dequeue();
                    }
                } while (openBrackets > 0);
                sepExpr = this.Parse(subtokens);
            }

            //Now we need to generate the actual expression
            switch (agg.TokenType)
            {
                case Token.AVG:
                    //AVG Aggregate
                    if (aggExpr is VariableExpressionTerm)
                    {
                        return new AggregateExpressionTerm(new AverageAggregate((VariableExpressionTerm)aggExpr, distinct));
                    }
                    else
                    {
                        return new AggregateExpressionTerm(new AverageAggregate(aggExpr, distinct));
                    }

                case Token.COUNT:
                    //COUNT Aggregate
                    if (all)
                    {
                        if (distinct)
                        {
                            return new AggregateExpressionTerm(new CountAllDistinctAggregate());
                        }
                        else
                        {
                            return new AggregateExpressionTerm(new CountAllAggregate());
                        }
                    }
                    else if (aggExpr is VariableExpressionTerm)
                    {
                        if (distinct)
                        {
                            return new AggregateExpressionTerm(new CountDistinctAggregate((VariableExpressionTerm)aggExpr));
                        }
                        else
                        {
                            return new AggregateExpressionTerm(new CountAggregate((VariableExpressionTerm)aggExpr));
                        }
                    }
                    else
                    {
                        if (distinct)
                        {
                            return new AggregateExpressionTerm(new CountDistinctAggregate(aggExpr));
                        }
                        else
                        {
                            return new AggregateExpressionTerm(new CountAggregate(aggExpr));
                        }
                    }
                case Token.GROUPCONCAT:
                    if (sepGroupConcat)
                    {
                        return new NonNumericAggregateExpressionTerm(new GroupConcatAggregate(aggExpr, sepExpr, distinct));
                    }
                    else
                    {
                        return new NonNumericAggregateExpressionTerm(new GroupConcatAggregate(aggExpr, distinct));
                    }

                case Token.MAX:
                    //MAX Aggregate
                    if (aggExpr is VariableExpressionTerm)
                    {
                        return new NonNumericAggregateExpressionTerm(new MaxAggregate((VariableExpressionTerm)aggExpr, distinct));
                    }
                    else
                    {
                        return new NonNumericAggregateExpressionTerm(new MaxAggregate(aggExpr, distinct));
                    }

                case Token.MEDIAN:
                    //MEDIAN Aggregate
                    if (this._syntax != SparqlQuerySyntax.Extended) throw new RdfParseException("The MEDIAN aggregate is only supported when the Syntax is set to Extended.");
                    if (aggExpr is VariableExpressionTerm)
                    {
                        return new NonNumericAggregateExpressionTerm(new MedianAggregate((VariableExpressionTerm)aggExpr, distinct));
                    }
                    else
                    {
                        return new NonNumericAggregateExpressionTerm(new MedianAggregate(aggExpr, distinct));
                    }

                case Token.MIN:
                    //MIN Aggregate
                    if (aggExpr is VariableExpressionTerm)
                    {
                        return new NonNumericAggregateExpressionTerm(new MinAggregate((VariableExpressionTerm)aggExpr, distinct));
                    }
                    else
                    {
                        return new NonNumericAggregateExpressionTerm(new MinAggregate(aggExpr, distinct));
                    }

                case Token.MODE:
                    //MODE Aggregate
                    if (this._syntax != SparqlQuerySyntax.Extended) throw new RdfParseException("The MODE aggregate is only supported when the Syntax is set to Extended.");
                    if (aggExpr is VariableExpressionTerm)
                    {
                        return new NonNumericAggregateExpressionTerm(new ModeAggregate((VariableExpressionTerm)aggExpr, distinct));
                    }
                    else
                    {
                        return new NonNumericAggregateExpressionTerm(new ModeAggregate(aggExpr, distinct));
                    }

                case Token.NMAX:
                    //NMAX Aggregate
                    if (this._syntax != SparqlQuerySyntax.Extended) throw new RdfParseException("The NMAX (Numeric Maximum) aggregate is only supported when the Syntax is set to Extended.  To achieve an equivalent result in SPARQL 1.0/1.1 apply a FILTER to your query so the aggregated variable is only literals of the desired numeric type");
                    if (aggExpr is VariableExpressionTerm)
                    {
                        return new AggregateExpressionTerm(new NumericMaxAggregate((VariableExpressionTerm)aggExpr, distinct));
                    }
                    else
                    {
                        return new AggregateExpressionTerm(new NumericMaxAggregate(aggExpr, distinct));
                    }

                case Token.NMIN:
                    //NMIN Aggregate
                    if (this._syntax != SparqlQuerySyntax.Extended) throw new RdfParseException("The NMIN (Numeric Minimum) aggregate is only supported when the Syntax is set to Extended.  To achieve an equivalent result in SPARQL 1.0/1.1 apply a FILTER to your query so the aggregated variable is only literals of the desired numeric type");
                    if (aggExpr is VariableExpressionTerm)
                    {
                        return new AggregateExpressionTerm(new NumericMinAggregate((VariableExpressionTerm)aggExpr, distinct));
                    }
                    else
                    {
                        return new AggregateExpressionTerm(new NumericMinAggregate(aggExpr, distinct));
                    }

                case Token.SAMPLE:
                    //SAMPLE Aggregate
                    if (distinct) throw new RdfParseException("DISTINCT modifier is not valid for the SAMPLE aggregate");
                    return new NonNumericAggregateExpressionTerm(new SampleAggregate(aggExpr));

                case Token.SUM:
                    //SUM Aggregate
                    if (distinct) throw new RdfParseException("DISTINCT modifier is not valid for the SUM aggregate");
                    if (aggExpr is VariableExpressionTerm)
                    {
                        return new AggregateExpressionTerm(new SumAggregate((VariableExpressionTerm)aggExpr));
                    }
                    else
                    {
                        return new AggregateExpressionTerm(new SumAggregate(aggExpr));
                    }

                default:
                    //Should have already handled this but have to have it to keep the compiler happy
                    throw Error("Cannot parse an Aggregate since '" + agg.GetType().ToString() + "' is not an Aggregate Keyword Token", agg);
            }
        }

        private ISparqlExpression TryParseSetExpression(VariableExpressionTerm varTerm, Queue<IToken> tokens)
        {
            IToken next = tokens.Dequeue();
            ISparqlExpression nodeExpr;
            bool inSet = (next.TokenType == Token.IN);
            List<INode> values = new List<INode>();

            //Expecting a ( afterwards
            next = tokens.Dequeue();
            if (next.TokenType == Token.LEFTBRACKET)
            {
                next = tokens.Peek();
                while (next.TokenType != Token.RIGHTBRACKET)
                {
                    switch (next.TokenType)
                    {
                        case Token.QNAME:
                        case Token.URI:
                            nodeExpr = this.TryParseIriRefOrFunction(tokens);
                            if (nodeExpr is NodeExpressionTerm)
                            {
                                values.Add(((NodeExpressionTerm)nodeExpr).Value(null,0));
                            }
                            else
                            {
                                throw new RdfParseException("Expected a URI/QName as a value for the set for an IN/NOT IN expression but got a " + nodeExpr.GetType().ToString());
                            }
                            break;
                        case Token.LITERAL:
                        case Token.LONGLITERAL:
                        case Token.LITERALWITHDT:
                        case Token.LITERALWITHLANG:
                            nodeExpr = this.TryParseRdfLiteral(tokens);
                            if (nodeExpr is NodeExpressionTerm)
                            {
                                values.Add(((NodeExpressionTerm)nodeExpr).Value(null,0));
                            }
                            else
                            {
                                throw new RdfParseException("Expected a Literal as a value for the set for an IN/NOT IN expression but got a " + nodeExpr.GetType().ToString());
                            }
                            break;
                        case Token.PLAINLITERAL:
                            nodeExpr = this.TryParseBooleanOrNumericLiteral(tokens);
                            if (nodeExpr is NodeExpressionTerm) 
                            {
                                values.Add(((NodeExpressionTerm)nodeExpr).Value(null,0));
                            } 
                            else 
                            {
                                throw new RdfParseException("Expected a Plain Literal as a value for the set for an IN/NOT IN expression but got a " + nodeExpr.GetType().ToString());
                            }
                            break;

                        default:
                            throw Error("Unexpected token '" + next.GetType().ToString() + "' encountered, expected a QName/URI/Literal as a value for the set for an IN/NOT IN expression", next);
                    }

                    next = tokens.Peek();
                }
                tokens.Dequeue();
            }
            else
            {
                throw Error("Expected a left bracket to start the set of values for an IN/NOT IN expression", next);
            }

            if (inSet)
            {
                return new SparqlInFunction(varTerm, values);
            }
            else
            {
                return new SparqlNotInFunction(varTerm, values);
            }
        }

        /// <summary>
        /// Helper method for raising informative standardised Parser Errors
        /// </summary>
        /// <param name="msg">The Error Message</param>
        /// <param name="t">The Token that is the cause of the Error</param>
        /// <returns></returns>
        private RdfParseException Error(String msg, IToken t)
        {
            StringBuilder output = new StringBuilder();
            output.Append("[");
            output.Append(t.GetType().ToString());
            output.Append(" at Line ");
            output.Append(t.StartLine);
            output.Append(" Column ");
            output.Append(t.StartPosition);
            output.Append(" to Line ");
            output.Append(t.EndLine);
            output.Append(" Column ");
            output.Append(t.EndPosition);
            output.Append("]\n");
            output.Append(msg);

            return new RdfParseException(output.ToString(), t);
        }
    }
}
