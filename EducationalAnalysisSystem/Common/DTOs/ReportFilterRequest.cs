using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs
{
    public class ReportFilterRequest
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        // kasnije ćemo dodati CourseId(s) kad uvedemo kurseve
        public Guid? StudentId { get; set; } // opcionalno, za fokus na jednog studenta
    }

}
