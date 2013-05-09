/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using VDS.RDF.Parsing;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF
{
    [TestFixture]
    public class GraphDiffTests
    {
        [Test]
        public void GraphDiffEqualGraphs()
        {
            Graph g = new Graph();
            Graph h = new Graph();
            FileLoader.Load(g, "resources\\InferenceTest.ttl");
            h = g;

            GraphDiffReport report = g.Difference(h);
            TestTools.ShowDifferences(report);

            Assert.IsTrue(report.AreEqual, "Graphs should be equal");
        }

        [Test]
        public void GraphDiffDifferentGraphs()
        {
            Graph g = new Graph();
            Graph h = new Graph();
            FileLoader.Load(g, "resources\\InferenceTest.ttl");
            FileLoader.Load(h, "resources\\Turtle.ttl");

            GraphDiffReport report = g.Difference(h);
            TestTools.ShowDifferences(report);

            Assert.IsFalse(report.AreEqual, "Graphs should not be equal");
        }

        [Test]
        public void GraphDiffEqualGraphs2()
        {
            Graph g = new Graph();
            Graph h = new Graph();
            FileLoader.Load(g, "resources\\InferenceTest.ttl");
            FileLoader.Load(h, "resources\\InferenceTest.ttl");

            GraphDiffReport report = g.Difference(h);
            TestTools.ShowDifferences(report);

            Assert.IsTrue(report.AreEqual, "Graphs should be equal");
        }

        [Test]
        public void GraphDiffRemovedGroundTriples()
        {
            Graph g = new Graph();
            Graph h = new Graph();
            FileLoader.Load(g, "resources\\InferenceTest.ttl");
            FileLoader.Load(h, "resources\\InferenceTest.ttl");

            //Remove Triples about Ford Fiestas from 2nd Graph
            h.Retract(h.GetTriplesWithSubject(new Uri("http://example.org/vehicles/FordFiesta")).ToList());

            GraphDiffReport report = g.Difference(h);
            TestTools.ShowDifferences(report);

            Assert.IsFalse(report.AreEqual, "Graphs should not have been reported as equal");
            Assert.IsTrue(report.RemovedTriples.Any(), "Difference should have reported some Removed Triples");
        }

        [Test]
        public void GraphDiffAddedGroundTriples()
        {
            Graph g = new Graph();
            Graph h = new Graph();
            FileLoader.Load(g, "resources\\InferenceTest.ttl");
            FileLoader.Load(h, "resources\\InferenceTest.ttl");

            //Add additional Triple to 2nd Graph
            IUriNode spaceVehicle = h.CreateUriNode("eg:SpaceVehicle");
            IUriNode subClass = h.CreateUriNode("rdfs:subClassOf");
            IUriNode vehicle = h.CreateUriNode("eg:Vehicle");
            h.Assert(new Triple(spaceVehicle, subClass, vehicle));

            GraphDiffReport report = g.Difference(h);
            TestTools.ShowDifferences(report);

            Assert.IsFalse(report.AreEqual, "Graphs should not have been reported as equal");
            Assert.IsTrue(report.AddedTriples.Any(), "Difference should have reported some Added Triples");
        }

        [Test]
        public void GraphDiffAddedMSG()
        {
            Graph g = new Graph();
            Graph h = new Graph();
            FileLoader.Load(g, "resources\\InferenceTest.ttl");
            FileLoader.Load(h, "resources\\InferenceTest.ttl");

            //Add additional Triple to 2nd Graph
            INode blank = h.CreateBlankNode();
            IUriNode subClass = h.CreateUriNode("rdfs:subClassOf");
            IUriNode vehicle = h.CreateUriNode("eg:Vehicle");
            h.Assert(new Triple(blank, subClass, vehicle));

            GraphDiffReport report = g.Difference(h);
            TestTools.ShowDifferences(report);

            Assert.IsFalse(report.AreEqual, "Graphs should not have been reported as equal");
            Assert.IsTrue(report.AddedMSGs.Any(), "Difference should have reported some Added MSGs");
        }

        [Test]
        public void GraphDiffRemovedMSG()
        {
            Graph g = new Graph();
            Graph h = new Graph();
            FileLoader.Load(g, "resources\\InferenceTest.ttl");
            FileLoader.Load(h, "resources\\InferenceTest.ttl");

            //Remove MSG from 2nd Graph
            INode toRemove = h.Nodes.BlankNodes().FirstOrDefault();
            if (toRemove == null) Assert.Inconclusive("No MSGs in test graph");
            h.Retract(h.GetTriplesWithSubject(toRemove).ToList());

            GraphDiffReport report = g.Difference(h);
            TestTools.ShowDifferences(report);

            Assert.IsFalse(report.AreEqual, "Graphs should not have been reported as equal");
            Assert.IsTrue(report.RemovedMSGs.Any(), "Difference should have reported some Removed MSGs");
        }

        [Test]
        public void GraphDiffNullReferenceBoth()
        {
            GraphDiff diff = new GraphDiff();
            GraphDiffReport report = diff.Difference(null, null);

            TestTools.ShowDifferences(report);

            Assert.IsTrue(report.AreEqual, "Graphs should have been reported as equal for two null references");
            Assert.IsFalse(report.AreDifferentSizes, "Graphs should have been reported same size for two null references");
        }

        [Test]
        public void GraphDiffNullReferenceA()
        {
            Graph g = new Graph();
            g.LoadFromFile("resources\\InferenceTest.ttl");

            GraphDiff diff = new GraphDiff();
            GraphDiffReport report = diff.Difference(null, g);
            TestTools.ShowDifferences(report);

            Assert.IsFalse(report.AreEqual, "Graphs should have been reported as non-equal for one null reference");
            Assert.IsTrue(report.AreDifferentSizes, "Graphs should have been reported as different sizes for one null reference");
            Assert.IsTrue(report.AddedTriples.Any(), "Report should list added triples");
        }

        [Test]
        public void GraphDiffNullReferenceB()
        {
            Graph g = new Graph();
            g.LoadFromFile("resources\\InferenceTest.ttl");

            GraphDiffReport report = g.Difference(null);
            TestTools.ShowDifferences(report);

            Assert.IsFalse(report.AreEqual, "Graphs should have been reported as non-equal for one null reference");
            Assert.IsTrue(report.AreDifferentSizes, "Graphs should have been reported as different sizes for one null reference");
            Assert.IsTrue(report.RemovedTriples.Any(), "Report should list removed triples");
        }

        [Test, Timeout(10000)]
        public void GraphDiffSlowOnEqualGraphsCase1()
        {
            const string testGraphName = "case1";
            TestGraphDiff(testGraphName);
        }

        [Test, Timeout(10000)]
        public void GraphDiffSlowOnEqualGraphsCase2()
        {
            const string testGraphName = "case2";
            TestGraphDiff(testGraphName);
        }

        [Test, Timeout(10000)]
        public void GraphDiffSlowOnEqualGraphsCase3()
        {
            const string testGraphName = "case3";
            TestGraphDiff(testGraphName);
        }

        [Test, Timeout(10000)]
        public void GraphDiffSlowOnEqualGraphsCase4()
        {
            const string testGraphName = "case4";
            TestGraphDiff(testGraphName);
        }

        [Test, Timeout(10000)]
        public void GraphDiffSlowOnEqualGraphsCase5()
        {
            const string testGraphName = "case5";
            TestGraphDiff(testGraphName);
        }

        [Test, Timeout(10000)]
        public void GraphDiffSlowOnEqualGraphsCase6()
        {
            const string testGraphName = "case6";
            TestGraphDiff(testGraphName);
        }

        private static void TestGraphDiff(string testGraphName)
        {
            Graph a = new Graph();
            a.LoadFromFile(string.Format("resources\\diff_cases\\{0}_a.ttl", testGraphName));
            Graph b = new Graph();
            b.LoadFromFile(string.Format("resources\\diff_cases\\{0}_b.ttl", testGraphName));

            var diff = a.Difference(b);

            if (!diff.AreEqual)
            {
                TestTools.ShowDifferences(diff);
            }
            Assert.IsTrue(diff.AreEqual);
        }
    }
}
