using System.Collections.Generic;
using System.Linq;

namespace Common.Model
{
   public class OfferingFileDto
   {
      public string FileName { get; set; } = string.Empty;
      public long FileSize { get; set; }
      /// <summary>
      /// Key: ipaddress:port
      /// Value: grade
      /// </summary>
      public Dictionary<string, int> EndpointsAndGrades { get; set; } = new Dictionary<string, int>();

      public OfferingFileDto()
      {

      }

      public OfferingFileDto(string EndPoint)
      {
         EndpointsAndGrades.Add(EndPoint, 0);
      }

      public void MergeWithAnotherOfferingFileDto(OfferingFileDto offeringFileDto)
      {
         foreach (KeyValuePair<string, int> keyValuePair in offeringFileDto.EndpointsAndGrades)
         {
            this.EndpointsAndGrades[keyValuePair.Key] = 0;            
         }

         //this.EndpointsAndGrades.Keys.Intersect(offeringFileDto.EndpointsAndGrades.Keys).ToList().ForEach(key => this.EndpointsAndGrades[key] = 0);
      }
   }
}
