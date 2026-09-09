using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Tests.Infrastructure;

[CollectionDefinition(Name)]
public class ApiTestCollection : ICollectionFixture<TaskFlowApiFixture>
{
        public const string Name = "Api";
}
