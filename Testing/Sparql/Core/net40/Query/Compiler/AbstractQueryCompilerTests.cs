﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using VDS.RDF.Graphs;
using VDS.RDF.Nodes;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Elements;
using VDS.RDF.Query.Results;

namespace VDS.RDF.Query.Compiler
{
    [TestFixture]
    public abstract class AbstractQueryCompilerTests
    {
        protected INodeFactory NodeFactory { get; set; }

        [SetUp]
        public void Setup()
        {
            if (this.NodeFactory == null) this.NodeFactory = new NodeFactory();
        }

        /// <summary>
        /// Creates a new query compiler instance to use for testing
        /// </summary>
        /// <returns></returns>
        protected abstract IQueryCompiler CreateInstance();

        [Test]
        public void QueryCompilerEmptyWhere()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Table), algebra);

            Table table = (Table) algebra;
            Assert.IsTrue(table.IsUnit);
        }

        [Test]
        public void QueryCompilerEmptyBgp()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            query.WhereClause = new TripleBlockElement(Enumerable.Empty<Triple>());

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Table), algebra);

            Table table = (Table) algebra;
            Assert.IsTrue(table.IsUnit);
        }

        [Test]
        public void QueryCompilerBgp()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            Triple t = new Triple(new VariableNode("s"), new VariableNode("p"), new VariableNode("o"));
            query.WhereClause = new TripleBlockElement(t.AsEnumerable());

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Bgp), algebra);

            Bgp bgp = (Bgp) algebra;
            Assert.AreEqual(1, bgp.TriplePatterns.Count);
            Assert.IsTrue(bgp.TriplePatterns.Contains(t));
        }

        [Test]
        public void QueryCompilerUnion1()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            Triple t = new Triple(new VariableNode("s"), new VariableNode("p"), new VariableNode("o"));
            IElement triples = new TripleBlockElement(t.AsEnumerable());

            IElement union = new UnionElement(triples.AsEnumerable().Concat(triples.AsEnumerable()));
            query.WhereClause = union;

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Union), algebra);

            Union u = (Union) algebra;
            Assert.IsInstanceOf(typeof (Bgp), u.Lhs);
            Assert.IsInstanceOf(typeof (Bgp), u.Rhs);
        }

        [Test]
        public void QueryCompilerUnion2()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            Triple t1 = new Triple(new VariableNode("a"), new VariableNode("b"), new VariableNode("c"));
            Triple t2 = new Triple(new VariableNode("d"), new VariableNode("e"), new VariableNode("f"));
            Triple t3 = new Triple(new VariableNode("g"), new VariableNode("h"), new VariableNode("i"));
            IElement triples1 = new TripleBlockElement(t1.AsEnumerable());
            IElement triples2 = new TripleBlockElement(t2.AsEnumerable());
            IElement triples3 = new TripleBlockElement(t3.AsEnumerable());
            IElement[] elements = {triples1, triples2, triples3};

            IElement union = new UnionElement(elements);
            query.WhereClause = union;

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Union), algebra);

            Union u = (Union) algebra;
            Assert.IsInstanceOf(typeof (Bgp), u.Lhs);

            Bgp bgp = (Bgp) u.Lhs;
            Assert.AreEqual(1, bgp.TriplePatterns.Count);
            Assert.IsTrue(bgp.TriplePatterns.Contains(t1));

            Assert.IsInstanceOf(typeof (Union), u.Rhs);
            u = (Union) u.Rhs;
            Assert.IsInstanceOf(typeof (Bgp), u.Lhs);

            bgp = (Bgp) u.Lhs;
            Assert.AreEqual(1, bgp.TriplePatterns.Count);
            Assert.IsTrue(bgp.TriplePatterns.Contains(t2));

            bgp = (Bgp) u.Rhs;
            Assert.AreEqual(1, bgp.TriplePatterns.Count);
            Assert.IsTrue(bgp.TriplePatterns.Contains(t3));
        }

        [Test]
        public void QueryCompilerInlineEmptyValues()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            IMutableTabularResults data = new MutableTabularResults(Enumerable.Empty<String>(), Enumerable.Empty<IMutableResultRow>());
            query.WhereClause = new DataElement(data);

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Table), algebra);

            Table table = (Table) algebra;
            Assert.IsTrue(table.IsEmpty);
        }

        [Test]
        public void QueryCompilerInlineValues1()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            IMutableTabularResults data = new MutableTabularResults("x".AsEnumerable(), Enumerable.Empty<IMutableResultRow>());
            data.Add(new MutableResultRow("x".AsEnumerable(), new Dictionary<string, INode> {{"x", 1.ToLiteral(this.NodeFactory)}}));
            query.WhereClause = new DataElement(data);

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Table), algebra);

            Table table = (Table) algebra;
            Assert.IsFalse(table.IsEmpty);
            Assert.IsFalse(table.IsUnit);

            Assert.AreEqual(1, table.Data.Count);
            Assert.IsTrue(table.Data.All(s => s.ContainsVariable("x")));
        }

        [Test]
        public void QueryCompilerInlineValues2()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            IMutableTabularResults data = new MutableTabularResults(new String[] {"x", "y"}, Enumerable.Empty<IMutableResultRow>());
            data.Add(new MutableResultRow("x".AsEnumerable(), new Dictionary<string, INode> {{"x", 1.ToLiteral(this.NodeFactory)}}));
            data.Add(new MutableResultRow("y".AsEnumerable(), new Dictionary<string, INode> {{"y", 2.ToLiteral(this.NodeFactory)}}));
            data.Add(new MutableResultRow(data.Variables, new Dictionary<string, INode> {{"x", 3.ToLiteral(this.NodeFactory)}, {"y", 4.ToLiteral(this.NodeFactory)}}));
            query.WhereClause = new DataElement(data);

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Table), algebra);

            Table table = (Table) algebra;
            Assert.IsFalse(table.IsEmpty);
            Assert.IsFalse(table.IsUnit);

            Assert.AreEqual(3, table.Data.Count);
            Assert.IsTrue(table.Data.All(s => s.ContainsVariable("x") || s.ContainsVariable("y")));
            Assert.IsFalse(table.Data.All(s => s.ContainsVariable("x") && s.ContainsVariable("y")));
            Assert.IsTrue(table.Data.Any(s => s.ContainsVariable("x") && s.ContainsVariable("y")));
        }

        [Test]
        public void QueryCompilerValues1()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            IMutableTabularResults data = new MutableTabularResults("x".AsEnumerable(), Enumerable.Empty<IMutableResultRow>());
            data.Add(new MutableResultRow("x".AsEnumerable(), new Dictionary<string, INode> {{"x", 1.ToLiteral(this.NodeFactory)}}));
            query.ValuesClause = data;

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Table), algebra);

            Table table = (Table) algebra;
            Assert.IsFalse(table.IsEmpty);
            Assert.IsFalse(table.IsUnit);

            Assert.AreEqual(1, table.Data.Count);
            Assert.IsTrue(table.Data.All(s => s.ContainsVariable("x")));
        }

        [Test]
        public void QueryCompilerValues2()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            IMutableTabularResults data = new MutableTabularResults(new String[] {"x", "y"}, Enumerable.Empty<IMutableResultRow>());
            data.Add(new MutableResultRow("x".AsEnumerable(), new Dictionary<string, INode> {{"x", 1.ToLiteral(this.NodeFactory)}}));
            data.Add(new MutableResultRow("y".AsEnumerable(), new Dictionary<string, INode> {{"y", 2.ToLiteral(this.NodeFactory)}}));
            data.Add(new MutableResultRow(data.Variables, new Dictionary<string, INode> {{"x", 3.ToLiteral(this.NodeFactory)}, {"y", 4.ToLiteral(this.NodeFactory)}}));
            query.ValuesClause = data;

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Table), algebra);

            Table table = (Table) algebra;
            Assert.IsFalse(table.IsEmpty);
            Assert.IsFalse(table.IsUnit);

            Assert.AreEqual(3, table.Data.Count);
            Assert.IsTrue(table.Data.All(s => s.ContainsVariable("x") || s.ContainsVariable("y")));
            Assert.IsFalse(table.Data.All(s => s.ContainsVariable("x") && s.ContainsVariable("y")));
            Assert.IsTrue(table.Data.Any(s => s.ContainsVariable("x") && s.ContainsVariable("y")));
        }

        [Test]
        public void QueryCompilerEmptyValues()
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            IMutableTabularResults data = new MutableTabularResults(Enumerable.Empty<String>(), Enumerable.Empty<IMutableResultRow>());
            query.ValuesClause = data;

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOf(typeof (Table), algebra);

            Table table = (Table) algebra;
            Assert.IsTrue(table.IsEmpty);
        }

        [TestCase(0),
         TestCase(100),
         TestCase(Int64.MaxValue),
         TestCase(-1),
         TestCase(Int64.MinValue)]
        public void QueryCompilerLimit(long limit)
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            query.Limit = limit;
            Assert.IsTrue(limit >= 0L ? query.HasLimit : !query.HasLimit);

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());

            if (limit >= 0L)
            {
                Assert.IsInstanceOf(typeof (Slice), algebra);

                Slice slice = (Slice) algebra;
                Assert.AreEqual(limit, slice.Limit);
                Assert.AreEqual(0L, slice.Offset);
            }
            else
            {
                Assert.IsInstanceOf(typeof (Table), algebra);

                Table table = (Table) algebra;
                Assert.IsTrue(table.IsUnit);
            }
        }

        [TestCase(0),
         TestCase(100),
         TestCase(Int64.MaxValue),
         TestCase(-1),
         TestCase(Int64.MinValue)]
        public void QueryCompilerOffset(long offset)
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            query.Offset = offset;
            Assert.IsTrue(offset > 0L ? query.HasOffset : !query.HasOffset);

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());

            if (offset > 0L)
            {
                Assert.IsInstanceOf(typeof (Slice), algebra);

                Slice slice = (Slice) algebra;
                Assert.AreEqual(offset, slice.Offset);
                Assert.AreEqual(-1L, slice.Limit);
            }
            else
            {
                Assert.IsInstanceOf(typeof (Table), algebra);

                Table table = (Table) algebra;
                Assert.IsTrue(table.IsUnit);
            }
        }

        [TestCase(0, 0),
         TestCase(100, 0),
         TestCase(100, 5000),
         TestCase(Int64.MaxValue, 0),
         TestCase(0, Int64.MaxValue),
         TestCase(-1, -1),
         TestCase(-1, 100),
         TestCase(Int64.MinValue, 0),
         TestCase(0, Int64.MinValue)]
        public void QueryCompilerLimitOffset(long limit, long offset)
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            query.Limit = limit;
            query.Offset = offset;
            Assert.IsTrue(limit >= 0L ? query.HasLimit : !query.HasLimit);
            Assert.IsTrue(offset > 0L ? query.HasOffset : !query.HasOffset);

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());

            if (limit >= 0L || offset > 0L)
            {
                Assert.IsInstanceOf(typeof (Slice), algebra);

                Slice slice = (Slice) algebra;
                Assert.AreEqual(limit >= 0L ? limit : -1L, slice.Limit);
                Assert.AreEqual(offset > 0L ? offset : 0L, slice.Offset);
            }
            else
            {
                Assert.IsInstanceOf(typeof (Table), algebra);

                Table table = (Table) algebra;
                Assert.IsTrue(table.IsUnit);
            }
        }

        [TestCase(QueryType.SelectAllDistinct),
         TestCase(QueryType.SelectDistinct)]
        public void QueryCompilerDistinct(QueryType type)
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            query.QueryType = type;

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());

            Assert.IsInstanceOf(typeof (Distinct), algebra);
        }

        [TestCase(QueryType.SelectAllReduced),
         TestCase(QueryType.SelectReduced)]
        public void QueryCompilerReduced(QueryType type)
        {
            IQueryCompiler compiler = this.CreateInstance();

            IQuery query = new Query();
            query.QueryType = type;

            IAlgebra algebra = compiler.Compile(query);
            Console.WriteLine(algebra.ToString());

            Assert.IsInstanceOf(typeof (Reduced), algebra);
        }
    }

    /// <summary>
    /// Tests for the <see cref="DefaultQueryCompiler"/>
    /// </summary>
    public class DefaultQueryCompilerTests
        : AbstractQueryCompilerTests
    {
        protected override IQueryCompiler CreateInstance()
        {
            return new DefaultQueryCompiler();
        }
    }
}