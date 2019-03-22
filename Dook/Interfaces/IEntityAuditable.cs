using System;
using System.Collections.Generic;

namespace Dook
{
    public interface IEntityAuditable : ITrackDateOfCreation, ITrackDateOfChange
    {
    }
}
