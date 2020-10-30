//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PmcExcelScheduleSync
{
    using System;
    using System.Collections.Generic;

    public partial class PrepareSchedule
    {
        public int id { get; set; }
        public string po { get; set; }
        public string modelName { get; set; }
        public int lineType_id { get; set; }
        public int totalQty { get; set; }
        public int scheduleQty { get; set; }
        public decimal timeCost { get; set; }
        public Nullable<int> line_id { get; set; }
        public Nullable<int> undoneQty { get; set; }
        public string memo { get; set; }
        public System.DateTime onboardDate { get; set; }
        public int floor_id { get; set; }
        public Nullable<int> priority { get; set; }
        public System.DateTime createDate { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is PrepareSchedule other)
            {
                if (po == other.po &&
                    modelName == other.modelName &&
                    lineType_id == other.lineType_id &&
                    scheduleQty == other.scheduleQty &&
                    timeCost == other.timeCost &&
                    onboardDate == other.onboardDate &&
                    floor_id == other.floor_id)
                    return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return po.GetHashCode() ^
                modelName.GetHashCode() ^
                lineType_id.GetHashCode() ^
                scheduleQty.GetHashCode() ^
                timeCost.GetHashCode() ^
                onboardDate.GetHashCode() ^
                floor_id.GetHashCode();
        }
    }
}
