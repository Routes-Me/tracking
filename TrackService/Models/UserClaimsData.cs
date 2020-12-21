using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrackService.Models
{
    public class UserClaimsData
    {
        public string Application { get; set; }
        public string Privilege { get; set; }
        public string TokenInstitutionId { get; set; }
    }
}
