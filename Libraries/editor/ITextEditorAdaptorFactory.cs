﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VDS.RDF.Utilities.Editor
{
    public interface ITextEditorAdaptorFactory<T>
    {
        ITextEditorAdaptor<T> CreateAdaptor();
    }
}
