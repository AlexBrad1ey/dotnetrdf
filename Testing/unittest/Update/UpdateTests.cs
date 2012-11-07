/*

Copyright dotNetRDF Project 2009-12
dotnetrdf-develop@lists.sf.net

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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Update;
using VDS.RDF.Update.Commands;

namespace VDS.RDF.Test
{
    [TestClass]
    public class UpdateTests
    {
        [TestMethod]
        public void SparqlUpdateCreateDrop()
        {
            TripleStore store = new TripleStore();

            Console.WriteLine("Store has " + store.Graphs.Count + " Graphs");

            //Create a couple of Graphs using Create Commands
            CreateCommand create1 = new CreateCommand(new Uri("http://example.org/1"));
            CreateCommand create2 = new CreateCommand(new Uri("http://example.org/2"));

            store.ExecuteUpdate(create1);
            store.ExecuteUpdate(create2);

            Assert.AreEqual(3, store.Graphs.Count, "Store should have now had three Graphs");
            Assert.AreEqual(0, store.Triples.Count(), "Store should have no triples at this point");

            //Trying the same Create again should cause an error
            try
            {
                store.ExecuteUpdate(create1);
                Assert.Fail("Executing a CREATE command twice without the SILENT modifier should error");
            }
            catch (SparqlUpdateException)
            {
                Console.WriteLine("Executing a CREATE command twice without the SILENT modifier errored as expected");
            }

            //Equivalent Create with SILENT should not error
            CreateCommand create3 = new CreateCommand(new Uri("http://example.org/1"), true);
            try
            {
                store.ExecuteUpdate(create3);
                Console.WriteLine("Executing a CREATE for an existing Graph with the SILENT modifier suppressed the error as expected");
            }
            catch (SparqlUpdateException)
            {
                Assert.Fail("Executing a CREATE for an existing Graph with the SILENT modifier should not error");
            }

            DropCommand drop1 = new DropCommand(new Uri("http://example.org/1"));
            store.ExecuteUpdate(drop1);
            Assert.AreEqual(2, store.Graphs.Count, "Store should have only 2 Graphs after we executed the DROP command");

            try
            {
                store.ExecuteUpdate(drop1);
                Assert.Fail("Trying to DROP a non-existent Graph should error");
            }
            catch (SparqlUpdateException)
            {
                Console.WriteLine("Trying to DROP a non-existent Graph produced an error as expected");
            }

            DropCommand drop2 = new DropCommand(new Uri("http://example.org/1"), ClearMode.Graph, true);
            try
            {
                store.ExecuteUpdate(drop2);
                Console.WriteLine("Trying to DROP a non-existent Graph with the SILENT modifier suppressed the error as expected");
            }
            catch (SparqlUpdateException)
            {
                Assert.Fail("Trying to DROP a non-existent Graph with the SILENT modifier should suppress the error");
            }
        }

        [TestMethod]
        public void SparqlUpdateLoad()
        {
            TripleStore store = new TripleStore();

            LoadCommand loadLondon = new LoadCommand(new Uri("http://dbpedia.org/resource/London"));
            LoadCommand loadSouthampton = new LoadCommand(new Uri("http://dbpedia.org/resource/Southampton"), new Uri("http://example.org"));

            store.ExecuteUpdate(loadLondon);
            store.ExecuteUpdate(loadSouthampton);

            Assert.AreEqual(2, store.Graphs.Count, "Should now be 2 Graphs in the Store");
            Assert.AreNotEqual(0, store.Triples.Count(), "Should be some Triples in the Store");

            foreach (IGraph g in store.Graphs)
            {
                foreach (Triple t in g.Triples)
                {
                    Console.Write(t.ToString());
                    if (g.BaseUri != null)
                    {
                        Console.WriteLine(" from " + g.BaseUri.ToString());
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
            }
        }

        [TestMethod]
        public void SparqlUpdateModify()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            g.BaseUri = null;
            store.Add(g);

            IUriNode rdfType = g.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));

            Assert.AreNotEqual(0, store.GetTriplesWithPredicate(rdfType).Count(), "Store should contain some rdf:type Triples");

            String update = "DELETE {?s a ?type} WHERE {?s a ?type}";
            SparqlUpdateParser parser = new SparqlUpdateParser();
            SparqlUpdateCommandSet cmds = parser.ParseFromString(update);
            store.ExecuteUpdate(cmds);

            Assert.AreEqual(0, store.GetTriplesWithPredicate(rdfType).Count(), "Store should contain no rdf:type Triples after DELETE command executes");
        }

        [TestMethod]
        public void SparqlUpdateWithCustomQueryProcessor()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");
            g.BaseUri = null;
            TripleStore store = new TripleStore();
            store.Add(g);
            InMemoryDataset dataset = new InMemoryDataset(store);

            SparqlUpdateParser parser = new SparqlUpdateParser();
            SparqlUpdateCommandSet cmds = parser.ParseFromString("DELETE { ?s a ?type } WHERE { ?s a ?type }");

            ExplainUpdateProcessor processor = new ExplainUpdateProcessor(dataset, ExplanationLevel.Full);
            processor.ProcessCommandSet(cmds);
        }

        [TestMethod]
        public void SparqlUpdateChangesNotReflectedInOriginalGraph1()
        {
            //Test Case originally submitted by Tomasz Pluskiewicz

            // given
            IGraph sourceGraph = new Graph();
            sourceGraph.LoadFromString(@"@prefix ex: <http://www.example.com/> .
ex:Subject ex:hasObject ex:Object .");

            IGraph expectedGraph = new Graph();
            expectedGraph.LoadFromString(@"@prefix ex: <http://www.example.com/> .
ex:Subject ex:hasBlank [ ex:hasObject ex:Object ] .");


            //IMPORTANT - Because we create the dataset without an existing store it
            //creates a new triple store internally which has a single unnamed graph
            //Then when we add another unnamed graph it merges into that existing graph,
            //this is why the update does not apply to the added graph but rather to
            //the merged graph that is internal to the dataset
            ISparqlDataset dataset = new InMemoryDataset(true);
            dataset.AddGraph(sourceGraph);

            // when
            var command = new SparqlParameterizedString
            {
                CommandText = @"PREFIX ex: <http://www.example.com/>
DELETE { ex:Subject ex:hasObject ex:Object . }
INSERT { ex:Subject ex:hasBlank [ ex:hasObject ex:Object ] . }
WHERE { ?s ?p ?o . }"
            };
            SparqlUpdateCommandSet cmds = new SparqlUpdateParser().ParseFromString(command);
            LeviathanUpdateProcessor processor = new LeviathanUpdateProcessor(dataset);
            processor.ProcessCommandSet(cmds);

            Assert.AreEqual(1, dataset.Graphs.Count(), "Should only be 1 Graph");

            IGraph g = dataset.Graphs.First();
            Assert.AreEqual(2, g.Triples.Count, "Result Graph should have 2 triples");
            Assert.IsFalse(ReferenceEquals(g, sourceGraph), "Result Graph should not be the Source Graph");
            Assert.AreEqual(1, sourceGraph.Triples.Count, "Source Graph should not be modified");
            Assert.AreNotEqual(expectedGraph, sourceGraph, "Source Graph should not match expected Graph");
        }

        [TestMethod]
        public void SparqlUpdateChangesNotReflectedInOriginalGraph2()
        {
            //Test Case originally submitted by Tomasz Pluskiewicz

            // given
            IGraph sourceGraph = new Graph();
            sourceGraph.LoadFromString(@"@prefix ex: <http://www.example.com/> .
ex:Subject ex:hasObject ex:Object .");

            IGraph expectedGraph = new Graph();
            expectedGraph.LoadFromString(@"@prefix ex: <http://www.example.com/> .
ex:Subject ex:hasBlank [ ex:hasObject ex:Object ] .");

            TripleStore store = new TripleStore();
            store.Add(sourceGraph);
            ISparqlDataset dataset = new InMemoryDataset(store, sourceGraph.BaseUri);

            // when
            var command = new SparqlParameterizedString
            {
                CommandText = @"PREFIX ex: <http://www.example.com/>
DELETE { ex:Subject ex:hasObject ex:Object . }
INSERT { ex:Subject ex:hasBlank [ ex:hasObject ex:Object ] . }
WHERE { ?s ?p ?o . }"
            };
            SparqlUpdateCommandSet cmds = new SparqlUpdateParser().ParseFromString(command);
            LeviathanUpdateProcessor processor = new LeviathanUpdateProcessor(dataset);
            processor.ProcessCommandSet(cmds);

            Assert.AreEqual(1, dataset.Graphs.Count(), "Should only be 1 Graph");

            IGraph g = dataset.Graphs.First();
            Assert.AreEqual(2, g.Triples.Count, "Result Graph should have 2 triples");
            Assert.IsTrue(ReferenceEquals(g, sourceGraph), "Result Graph should be the Source Graph");
            Assert.AreEqual(2, sourceGraph.Triples.Count, "Source Graph should have be modified and now contain 2 triples");
            Assert.AreEqual(expectedGraph, sourceGraph, "Source Graph should match expected Graph");
        }

        [TestMethod]
        public void SparqlUpdateInsertDeleteWithBlankNodes()
        {
            //This test adapted from a contribution by Tomasz Pluskiewicz
            //It demonstrates an issue in SPARQL Update caused by incorrect Graph
            //references that can result when using GraphPersistenceWrapper in a SPARQL Update
            String initData = @"@prefix ex: <http://www.example.com/>.
@prefix rr: <http://www.w3.org/ns/r2rml#>.

ex:triplesMap rr:predicateObjectMap _:blank .
_:blank rr:object ex:Employee, ex:Worker .";

            String update = @"PREFIX rr: <http://www.w3.org/ns/r2rml#>

DELETE { ?map rr:object ?value . }
INSERT { ?map rr:objectMap [ rr:constant ?value ] . }
WHERE { ?map rr:object ?value }";

            String query = @"prefix ex: <http://www.example.com/>
prefix rr: <http://www.w3.org/ns/r2rml#>

select *
where
{
ex:triplesMap rr:predicateObjectMap ?predObjMap .
?predObjMap rr:objectMap ?objMap .
}";

            String expectedData = @"@prefix ex: <http://www.example.com/>.
@prefix rr: <http://www.w3.org/ns/r2rml#>.

ex:triplesMap rr:predicateObjectMap _:blank.
_:blank rr:objectMap _:autos1.
_:autos1 rr:constant ex:Employee.
_:autos2 rr:constant ex:Worker.
_:blank rr:objectMap _:autos2.";

            // given
            IGraph graph = new Graph();
            graph.LoadFromString(initData);
            IGraph expectedGraph = new Graph();
            expectedGraph.LoadFromString(expectedData);

            Console.WriteLine("Initial Graph:");
            TestTools.ShowGraph(graph);
            Console.WriteLine();

            // when
            TripleStore store = new TripleStore();
            store.Add(graph);

            var dataset = new InMemoryDataset(store, graph.BaseUri);
            ISparqlUpdateProcessor processor = new LeviathanUpdateProcessor(dataset);
            var updateParser = new SparqlUpdateParser();
            processor.ProcessCommandSet(updateParser.ParseFromString(update));

            Console.WriteLine("Resulting Graph:");
            TestTools.ShowGraph(graph);
            Console.WriteLine();

            Triple x = graph.GetTriplesWithPredicate(graph.CreateUriNode("rr:predicateObjectMap")).FirstOrDefault();
            INode origBNode = x.Object;
            Assert.IsTrue(graph.GetTriples(origBNode).Count() > 1, "Should be more than one Triple using the BNode");
            IEnumerable<Triple> ys = graph.GetTriplesWithSubject(origBNode);
            foreach (Triple y in ys)
            {
                Assert.AreEqual(origBNode, y.Subject, "Blank Nodes should be equal");
            }

            //Graphs should be equal
            GraphDiffReport diff = graph.Difference(expectedGraph);
            if (!diff.AreEqual) TestTools.ShowDifferences(diff);
            Assert.AreEqual(expectedGraph, graph, "Graphs should be equal");

            //Test the Query
            SparqlResultSet results = graph.ExecuteQuery(query) as SparqlResultSet;
            TestTools.ShowResults(results);
            Assert.IsFalse(results.IsEmpty, "Should be some results");
        }

        [TestMethod]
        public void SparqlUpdateInsertBNodesComplex1()
        {
            String update = @"PREFIX : <http://test/>
INSERT { :s :p _:b } WHERE { };
INSERT { ?o ?p ?s } WHERE { ?s ?p ?o }";

            TripleStore store = new TripleStore();
            store.ExecuteUpdate(update);

            Assert.AreEqual(1, store.Graphs.Count);
            Assert.AreEqual(2, store.Triples.Count());

            IGraph def = store[null];
            Triple a = def.Triples.Where(t => t.Subject.NodeType == NodeType.Blank).FirstOrDefault();
            Assert.IsNotNull(a);
            Triple b = def.Triples.Where(t => t.Object.NodeType == NodeType.Blank).FirstOrDefault();
            Assert.IsNotNull(b);

            Assert.AreEqual(a.Subject, b.Object);
        }

        [TestMethod]
        public void SparqlUpdateInsertBNodesComplex2()
        {
            String update = @"PREFIX : <http://test/>
INSERT { GRAPH :a { :s :p _:b } } WHERE { };
INSERT { GRAPH :b { :s :p _:b } } WHERE { };
INSERT { GRAPH :a { ?s ?p ?o } } WHERE { GRAPH :b { ?s ?p ?o } }";

            TripleStore store = new TripleStore();
            store.ExecuteUpdate(update);

            Assert.AreEqual(3, store.Graphs.Count, "Expected 3 Graphs");
            Assert.AreEqual(2, store[new Uri("http://test/a")].Triples.Count, "Expected 2 Triples");

        }

        [TestMethod]
        public void SparqlUpdateInsertBNodesComplex3()
        {
            String update = @"PREFIX : <http://test/>
INSERT DATA { GRAPH :a { :s :p _:b } GRAPH :b { :s :p _:b } };
INSERT { GRAPH :a { ?s ?p ?o } } WHERE { GRAPH :b { ?s ?p ?o } }";

            TripleStore store = new TripleStore();
            store.ExecuteUpdate(update);

            Assert.AreEqual(3, store.Graphs.Count, "Expected 3 Graphs");
            Assert.AreEqual(1, store[new Uri("http://test/a")].Triples.Count, "Expected 1 Triple");

        }
    }
}
