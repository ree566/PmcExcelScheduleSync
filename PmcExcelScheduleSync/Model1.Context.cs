﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class ATMCEntities : DbContext
    {
        public ATMCEntities()
            : base("name=ATMCEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<PrepareSchedule> PrepareSchedule { get; set; }
        public virtual DbSet<vTb_WorkTime> vTb_WorkTime { get; set; }
        public virtual DbSet<LineType> LineType { get; set; }
        public virtual DbSet<PrepareScheduleRemark_PMC> PrepareScheduleRemark_PMC { get; set; }
    }
}
