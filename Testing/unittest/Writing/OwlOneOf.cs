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
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Test.Writing
{
    [TestClass]
    public class OwlOneOf
    {
        [TestMethod]
        public void WritingSerializeOwnOneOf()
        {
            //Create the Graph for the Test and Generate a List of URIs
            Graph g = new Graph();
            List<IUriNode> nodes = new List<IUriNode>();
            for (int i = 1; i <= 10; i++)
            {
                nodes.Add(g.CreateUriNode(new Uri("http://example.org/Class" + i)));
            }

            //Use the thingOneOf to generate the Triples
            thingOneOf(g, nodes.ToArray());

            //Dump as NTriples to the Console
            NTriplesFormatter formatter = new NTriplesFormatter();
            foreach (Triple t in g.Triples)
            {
                Console.WriteLine(t.ToString(formatter));
            }

            Console.WriteLine();

            //Now try to save as RDF/XML
            IRdfWriter writer = new RdfXmlWriter();
            writer.Save(g, "owl-one-of.rdf");
            Console.WriteLine("Saved OK using RdfXmlWriter");
            Console.WriteLine();

            writer = new PrettyRdfXmlWriter();
            writer.Save(g, "owl-one-of-pretty.rdf");
            Console.WriteLine("Saved OK using PrettyRdfXmlWriter");
            Console.WriteLine();

            //Now check that the Graphs are all equivalent
            Graph h = new Graph();
            FileLoader.Load(h, "owl-one-of.rdf");
            Assert.AreEqual(g, h, "Graphs should be equal (RdfXmlWriter)");
            Console.WriteLine("RdfXmlWriter serialization was OK");
            Console.WriteLine();

            Graph j = new Graph();
            FileLoader.Load(j, "owl-one-of-pretty.rdf");
            Assert.AreEqual(g, j, "Graphs should be equal (PrettyRdfXmlWriter)");
            Console.WriteLine("PrettyRdfXmlWriter serialization was OK");
        }

        [TestMethod]
        public void WritingSerializeOwnOneOfVeryLarge()
        {
                //Create the Graph for the Test and Generate a List of URIs
                Graph g = new Graph();
                List<IUriNode> nodes = new List<IUriNode>();
                for (int i = 1; i <= 10000; i++)
                {
                    nodes.Add(g.CreateUriNode(new Uri("http://example.org/Class" + i)));
                }

                //Use the thingOneOf to generate the Triples
                thingOneOf(g, nodes.ToArray());

                //Dump as NTriples to the Console
                NTriplesFormatter formatter = new NTriplesFormatter();
                foreach (Triple t in g.Triples)
                {
                    Console.WriteLine(t.ToString(formatter));
                }

                Console.WriteLine();

                //Now try to save as RDF/XML
                IRdfWriter writer = new RdfXmlWriter();
                writer.Save(g, "owl-one-of.rdf");
                
                Console.WriteLine("Saved OK using RdfXmlWriter");
                Console.WriteLine();

                writer = new PrettyRdfXmlWriter();
                ((ICompressingWriter)writer).CompressionLevel = WriterCompressionLevel.Medium;
                writer.Save(g, "owl-one-of-pretty.rdf");
                Console.WriteLine("Saved OK using PrettyRdfXmlWriter");
                Console.WriteLine();

                //Now check that the Graphs are all equivalent
                Graph h = new Graph();
                FileLoader.Load(h, "owl-one-of.rdf");
                Assert.AreEqual(g, h, "Graphs should be equal (RdfXmlWriter)");
                Console.WriteLine("RdfXmlWriter serialization was OK");
                Console.WriteLine();

                Graph j = new Graph();
                FileLoader.Load(j, "owl-one-of-pretty.rdf");
                Assert.AreEqual(g, j, "Graphs should be equal (PrettyRdfXmlWriter)");
                Console.WriteLine("PrettyRdfXmlWriter serialization was OK");
        }

        public static void thingOneOf(IGraph graph, IUriNode[] listInds)
        {
            IBlankNode oneOfNode = graph.CreateBlankNode();
            IBlankNode chainA = graph.CreateBlankNode();
            IUriNode rdfType = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode rdfFirst = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfListFirst));
            IUriNode rdfRest = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfListRest));
            IUriNode rdfNil = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfListNil));
            IUriNode owlClass = graph.CreateUriNode(new Uri(NamespaceMapper.OWL + "Class"));
            IUriNode owlOneOf = graph.CreateUriNode(new Uri(NamespaceMapper.OWL + "oneOf"));
            IUriNode owlThing = graph.CreateUriNode(new Uri(NamespaceMapper.OWL + "Thing"));
            IUriNode owlEquivClass = graph.CreateUriNode(new Uri(NamespaceMapper.OWL + "equivalentClass"));

            graph.Assert(new Triple(oneOfNode, rdfType, owlClass));
            graph.Assert(new Triple(oneOfNode, owlOneOf, chainA));
            graph.Assert(new Triple(owlThing, owlEquivClass, oneOfNode));

            for (int i = 0; i < listInds.Length; i++)
            {
                graph.Assert(new Triple(chainA, rdfFirst, listInds[i]));
                IBlankNode chainB = graph.CreateBlankNode();

                if (i < listInds.Length - 1)
                {
                    graph.Assert(new Triple(chainA, rdfRest, chainB));
                    chainA = chainB;
                }
            }
            graph.Assert(new Triple(chainA, rdfRest, rdfNil));
        }
    }

}
