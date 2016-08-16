﻿using System.Linq;
using FubuCore;
using Xunit;
using Shouldly;
using ST.Docs;
using ST.Docs.Samples;

namespace StoryTeller.Testing.ST.Docs.Samples
{
    
    public class EmbeddedSpecData_smoke_tester
    {
        private EmbeddedSpecData theEmbeddedData;

        public EmbeddedSpecData_smoke_tester()
        {
            var settings = new DocSettings
            {
                Root = ".".ToFullPath()
            };

            theEmbeddedData = new EmbeddedSpecData(settings);
        }


        [Fact]
        public void can_fetch_data_for_a_spec_path()
        {
            var data = theEmbeddedData.DataForPath("Sets/Arrays");

            data.ShouldNotBeNull();
            data.Fixtures.ShouldNotBeNull();
            data.Specification.ShouldNotBeNull();
            data.Results.ShouldNotBeNull();
        }

        [Fact]
        public void filters_the_fixtures_to_only_the_ones_used_by_the_spec()
        {
            var data = theEmbeddedData.DataForPath("Sets/Arrays");
            data.Fixtures.Single().key.ShouldBe("NameArray");
        }
    }
}