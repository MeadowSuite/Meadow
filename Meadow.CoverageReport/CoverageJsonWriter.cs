using Meadow.Core.Utils;
using Meadow.CoverageReport.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Meadow.CoverageReport
{
    class CoverageJsonWriter
    {
        readonly IndexViewModel _indexViewModel;

        public CoverageJsonWriter(IndexViewModel indexViewModel)
        {
            _indexViewModel = indexViewModel;
        }

        public void WriteJson(Stream outputStream)
        {
            var serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            serializer.Culture = CultureInfo.InvariantCulture;
            serializer.Converters.Add(new StringEnumConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (var streamWriter = new StreamWriter(outputStream, StringUtil.UTF8, bufferSize: 1024, leaveOpen: true))
            using (var jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                serializer.Serialize(jsonTextWriter, _indexViewModel);
            }
        }
    }
}
