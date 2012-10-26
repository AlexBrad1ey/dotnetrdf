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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lucene.Net.Search;
using VDS.RDF.Query;
using VDS.RDF.Query.FullText;
using VDS.RDF.Query.FullText.Indexing;
using VDS.RDF.Query.FullText.Indexing.Lucene;
using VDS.RDF.Query.FullText.Schema;
using VDS.RDF.Query.FullText.Search.Lucene;

namespace VDS.RDF.Test.Query.FullText
{
    [TestClass]
    public class IndexCreationTests
    {
        private IGraph GetTestData()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");
            return g;
        }

        [TestMethod]
        public void FullTextIndexCreationLuceneObjects()
        {
            IFullTextIndexer indexer = null;
            try
            {
                indexer = new LuceneObjectsIndexer(LuceneTestHarness.Index, LuceneTestHarness.Analyzer, LuceneTestHarness.Schema);
                indexer.Index(this.GetTestData());
            }
            finally
            {
                if (indexer != null) indexer.Dispose();
            }
        }

        [TestMethod]
        public void FullTextIndexCreationLuceneSubjects()
        {
            IFullTextIndexer indexer = null;
            try
            {
                indexer = new LuceneSubjectsIndexer(LuceneTestHarness.Index, LuceneTestHarness.Analyzer, LuceneTestHarness.Schema);
                indexer.Index(this.GetTestData());
            }
            finally
            {
                if (indexer != null) indexer.Dispose();
            }
        }

        [TestMethod]
        public void FullTextIndexCreationLucenePredicates()
        {
            IFullTextIndexer indexer = null;
            try
            {
                indexer = new LucenePredicatesIndexer(LuceneTestHarness.Index, LuceneTestHarness.Analyzer, LuceneTestHarness.Schema);
                indexer.Index(this.GetTestData());
            }
            finally
            {
                if (indexer != null) indexer.Dispose();
            }
        }

        [TestMethod]
        public void FullTextIndexDestructionLuceneSubjects()
        {
            IFullTextIndexer indexer = null;
            try
            {
                LuceneSearchProvider provider = null;
                int origCount;
                try
                {
                    provider = new LuceneSearchProvider(LuceneTestHarness.LuceneVersion, LuceneTestHarness.Index);
                    origCount = provider.Match("http").Count();
                    provider.Dispose();
                }
                catch
                {
                    origCount = 0;
                }
                finally
                {
                    if (provider != null) provider.Dispose();
                }
                Console.WriteLine("Prior to indexing search returns " + origCount + " result(s)");

                indexer = new LuceneSubjectsIndexer(LuceneTestHarness.Index, LuceneTestHarness.Analyzer, LuceneTestHarness.Schema);
                IGraph g = this.GetTestData();
                indexer.Index(g);
                indexer.Dispose();
                indexer = null;

                int currCount;
                try
                {
                    provider = new LuceneSearchProvider(LuceneTestHarness.LuceneVersion, LuceneTestHarness.Index);
                    currCount = provider.Match("http").Count();
                    provider.Dispose();
                }
                catch
                {
                    currCount = 0;
                }
                finally
                {
                    if (provider != null) provider.Dispose();
                }
                Console.WriteLine("After indexing search returns " + currCount + " result(s)");

                indexer = new LuceneSubjectsIndexer(LuceneTestHarness.Index, LuceneTestHarness.Analyzer, LuceneTestHarness.Schema);
                indexer.Unindex(g);
                indexer.Dispose();
                indexer = null;
                try
                {
                    provider = new LuceneSearchProvider(LuceneTestHarness.LuceneVersion, LuceneTestHarness.Index);
                    currCount = provider.Match("http").Count();
                    Console.WriteLine("After unindexing search returns " + currCount + " result(s)");
                }
                finally
                {
                    if (provider != null) provider.Dispose();
                }

                Assert.AreEqual(origCount, currCount, "Current Count should match original count");
            }
            finally
            {
                if (indexer != null) indexer.Dispose();
                LuceneTestHarness.Index.Dispose();
            }
        }

        [TestMethod]
        public void FullTextIndexDestructionLuceneObjects()
        {
            IFullTextIndexer indexer = null;
            try
            {
                LuceneSearchProvider provider = null;
                int origCount;
                try
                {
                    provider = new LuceneSearchProvider(LuceneTestHarness.LuceneVersion, LuceneTestHarness.Index);
                    origCount = provider.Match("http").Count();
                    provider.Dispose();
                }
                catch
                {
                    origCount = 0;
                }
                finally
                {
                    if (provider != null) provider.Dispose();
                }
                Console.WriteLine("Prior to indexing search returns " + origCount + " result(s)");

                indexer = new LuceneObjectsIndexer(LuceneTestHarness.Index, LuceneTestHarness.Analyzer, LuceneTestHarness.Schema);
                IGraph g = this.GetTestData();
                indexer.Index(g);
                indexer.Dispose();
                indexer = null;

                int currCount;
                try
                {
                    provider = new LuceneSearchProvider(LuceneTestHarness.LuceneVersion, LuceneTestHarness.Index);
                    currCount = provider.Match("http").Count();
                    provider.Dispose();
                }
                catch
                {
                    currCount = 0;
                }
                finally
                {
                    if (provider != null) provider.Dispose();
                }
                Console.WriteLine("After indexing search returns " + currCount + " result(s)");

                indexer = new LuceneObjectsIndexer(LuceneTestHarness.Index, LuceneTestHarness.Analyzer, LuceneTestHarness.Schema);
                indexer.Unindex(g);
                indexer.Dispose();
                indexer = null;
                try
                {
                    provider = new LuceneSearchProvider(LuceneTestHarness.LuceneVersion, LuceneTestHarness.Index);
                    currCount = provider.Match("http").Count();
                    Console.WriteLine("After unindexing search returns " + currCount + " result(s)");
                }
                finally
                {
                    if (provider != null) provider.Dispose();
                }

                Assert.AreEqual(origCount, currCount, "Current Count should match original count");
            }
            finally
            {
                if (indexer != null) indexer.Dispose();
                LuceneTestHarness.Index.Dispose();
            }
        }

        [TestMethod]
        public void FullTextIndexDestructionLucenePredicates()
        {
            IFullTextIndexer indexer = null;
            try
            {
                LuceneSearchProvider provider = null;
                int origCount;
                try
                {
                    provider = new LuceneSearchProvider(LuceneTestHarness.LuceneVersion, LuceneTestHarness.Index);
                    origCount = provider.Match("http").Count();
                    provider.Dispose();
                }
                catch
                {
                    origCount = 0;
                }
                finally
                {
                    if (provider != null) provider.Dispose();
                }
                Console.WriteLine("Prior to indexing search returns " + origCount + " result(s)");

                indexer = new LucenePredicatesIndexer(LuceneTestHarness.Index, LuceneTestHarness.Analyzer, LuceneTestHarness.Schema);
                IGraph g = this.GetTestData();
                indexer.Index(g);
                indexer.Dispose();
                indexer = null;

                int currCount;
                try
                {
                    provider = new LuceneSearchProvider(LuceneTestHarness.LuceneVersion, LuceneTestHarness.Index);
                    currCount = provider.Match("http").Count();
                    provider.Dispose();
                }
                catch
                {
                    currCount = 0;
                }
                finally
                {
                    if (provider != null) provider.Dispose();
                }
                Console.WriteLine("After indexing search returns " + currCount + " result(s)");

                indexer = new LucenePredicatesIndexer(LuceneTestHarness.Index, LuceneTestHarness.Analyzer, LuceneTestHarness.Schema);
                indexer.Unindex(g);
                indexer.Dispose();
                indexer = null;
                try
                {
                    provider = new LuceneSearchProvider(LuceneTestHarness.LuceneVersion, LuceneTestHarness.Index);
                    currCount = provider.Match("http").Count();
                    Console.WriteLine("After unindexing search returns " + currCount + " result(s)");
                }
                finally
                {
                    if (provider != null) provider.Dispose();
                }

                Assert.AreEqual(origCount, currCount, "Current Count should match original count");
            }
            finally
            {
                if (indexer != null) indexer.Dispose();
                LuceneTestHarness.Index.Dispose();
            }
        }

        [TestMethod]
        public void FullTextIndexMultiOccurrenceRemoval()
        {
            IFullTextIndexer indexer = null;
            try
            {
                indexer = new LuceneObjectsIndexer(LuceneTestHarness.Index, LuceneTestHarness.Analyzer, new DefaultIndexSchema());
                
                Graph g = new Graph();
                INode example = g.CreateLiteralNode("This is an example node which we'll index multiple times");

                for (int i = 0; i < 10; i++)
                {
                    g.Assert(new Triple(g.CreateBlankNode(), g.CreateUriNode(UriFactory.Create("ex:predicate")), example));
                }
                indexer.Index(g);

                LuceneSearchProvider searcher = new LuceneSearchProvider(LuceneTestHarness.LuceneVersion, LuceneTestHarness.Index);
                Assert.AreEqual(10, searcher.Match("example").Count(), "Expected 10 Results");

                for (int i = 9; i >= 0; i--)
                {
                    indexer.Unindex(g.Triples.First());
                    indexer.Flush();
                    Assert.AreEqual(i, searcher.Match("example").Count(), "Expected " + i + " Results as only one occurrence should have been unindexed");
                }
            }
            finally
            {
                if (indexer != null) indexer.Dispose();
            }
        }
    }
}
