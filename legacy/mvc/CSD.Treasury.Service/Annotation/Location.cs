using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.Service.LocationsService
{
    [MetadataType(typeof(Location_Validation))]
    public partial class Location
    {

    }
    public partial class Location_Validation
    {
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string LocationName { get; set; }

    }
}
