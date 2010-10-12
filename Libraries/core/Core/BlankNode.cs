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

namespace VDS.RDF
{
    /// <summary>
    /// Class for representing Blank RDF Nodes
    /// </summary>
    public class BlankNode : BaseNode, IComparable<BlankNode>
    {
        private String _id;
        private bool _autoassigned;

        /// <summary>
        /// Internal Only Constructor for Blank Nodes
        /// </summary>
        /// <param name="g">Graph this Node belongs to</param>
        protected internal BlankNode(IGraph g)
            : base(g, NodeType.Blank)
        {
            this._id = this._graph.GetNextBlankNodeID();
            this._autoassigned = true;

            //Compute Hash Code
            this._hashcode = (this._nodetype + this.ToString()).GetHashCode();
        }

        /// <summary>
        /// Internal Only constructor for Blank Nodes
        /// </summary>
        /// <param name="g">Graph this Node belongs to</param>
        /// <param name="nodeId">Custom Node ID to use</param>
        protected internal BlankNode(IGraph g, String nodeId)
            : base(g, NodeType.Blank)
        {
            this._id = nodeId;
            this._autoassigned = false;

            //Compute Hash Code
            this._hashcode = (this._nodetype + this.ToString()).GetHashCode();
        }

        /// <summary>
        /// Returns the Internal Blank Node ID this Node has in the Graph
        /// </summary>
        /// <remarks>
        /// Usually automatically assigned and of the form autosXXX where XXX is some number.  If an RDF document contains a Blank Node ID of this form that clashes with an existing auto-assigned ID it will be automatically remapped by the Graph using the <see cref="BlankNodeMapper">BlankNodeMapper</see> when it is created.
        /// </remarks>
        public String InternalID
        {
            get
            {
                return this._id;
            }
        }

        /// <summary>
        /// Indicates whether this Blank Node had its ID assigned for it by the Graph
        /// </summary>
        public bool HasAutoAssignedID
        {
            get
            {
                return this._autoassigned;
            }
        }

        /// <summary>
        /// Implementation of Equals for Blank Nodes
        /// </summary>
        /// <param name="obj">Object to compare with the Blank Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Blank Nodes are considered equal if their internal IDs match precisely and they originate from the same Graph
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            if (ReferenceEquals(this, obj)) return true;

            if (obj is INode)
            {
                return this.Equals((INode)obj);
            }
            else
            {
                //Can only be equal to things which are Nodes
                return false;
            }
        }

        /// <summary>
        /// Implementation of Equals for Blank Nodes
        /// </summary>
        /// <param name="other">Object to compare with the Blank Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Blank Nodes are considered equal if their internal IDs match precisely and they originate from the same Graph
        /// </remarks>
        public override bool Equals(INode other)
        {
            if ((Object)other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            if (other.NodeType == NodeType.Blank)
            {
                BlankNode temp = (BlankNode)other;

                //Only equal if our internal IDs match and we come from the same Graph
                return this._id.Equals(temp.InternalID) && ReferenceEquals(this._graph, temp.Graph);
            }
            else
            {
                //Can only be equal to Blank Nodes
                return false;
            }
        }

        /// <summary>
        /// Returns a string representation of this Blank Node in QName form
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "_:" + this._id;
        }

        /// <summary>
        /// Returns an Integer indicating the Ordering of this Node compared to another Node
        /// </summary>
        /// <param name="other">Node to test against</param>
        /// <returns></returns>
        public override int CompareTo(INode other)
        {
            if (other == null)
            {
                //Blank Nodes are considered greater than nulls
                //So we return a 1 to indicate we're greater than it
                return 1;
            }
            else if (other.NodeType == NodeType.Variable)
            {
                //Blank Nodes are considered greater than Variables
                return 1;
            }
            else if (other.NodeType == NodeType.Blank)
            {
                if (ReferenceEquals(this, other)) return 0;

                //Order Blank Nodes lexically by their ID
                return this.InternalID.CompareTo(((BlankNode)other).InternalID);
            }
            else
            {
                //Anything else is greater than a Blank Node
                //So we return a -1 to indicate we are less than the other Node
                return -1;
            }
        }

        /// <summary>
        /// Implementation of Compare To for Blank Nodes
        /// </summary>
        /// <param name="other">Blank Node to Compare To</param>
        /// <returns></returns>
        /// <remarks>
        /// Simply invokes the more general implementation of this method
        /// </remarks>
        public int CompareTo(BlankNode other)
        {
            return this.CompareTo((INode)other);
        }
    }
}
