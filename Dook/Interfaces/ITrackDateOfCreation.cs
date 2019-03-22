using System;
using System.Collections.Generic;

namespace Dook
{
    public interface ITrackDateOfCreation : IEntity
    {
        DateTime CreatedOn { get; set; }
    }
}
