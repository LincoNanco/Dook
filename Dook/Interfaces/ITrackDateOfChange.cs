using System;
using System.Collections.Generic;

namespace Dook
{
    public interface ITrackDateOfChange : IEntity
    {
        DateTime UpdatedOn { get; set; }
    }
}
