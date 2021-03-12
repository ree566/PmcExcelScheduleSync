using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExcelDataReader;

namespace PmcExcelScheduleSync
{
    public partial class Form1 : Form
    {
        static public string AppPath;

        private ATMCEntities db = new ATMCEntities();

        private List<FileMappingSetting> settings;

        private List<vTb_WorkTime> worktimes;

        private enum LINETYPE
        {
            PREASSY = 9,
            ASSY = 1,
            TEST_T1 = 7,
            TEST_T2 = 8,
            PACKING = 3
        }

        class FileMappingSetting
        {
            public Dictionary<LINETYPE, string> sheetLineTypeMappings { get; set; }
            public string filePath { get; set; }
            public int floorId { get; set; }

        }

        public Form1()
        {
            InitializeComponent();
        }

        private void initExcelParams()
        {
            worktimes = db.vTb_WorkTime.ToList();
            settings = new List<FileMappingSetting>();

            settings.Add(new FileMappingSetting()
            {
                sheetLineTypeMappings = new Dictionary<LINETYPE, string>() {
                    { LINETYPE.PREASSY, "5F--前置&組裝" },
                    { LINETYPE.ASSY, "5F--前置&組裝" }
                },
                filePath = @"\\aclfile2.advantech.corp\Group1\DF\PMC\生產日排程\APS 5F 組裝排程.xlsx",
                floorId = 2
            });

            settings.Add(new FileMappingSetting()
            {
                sheetLineTypeMappings = new Dictionary<LINETYPE, string>() {
                    { LINETYPE.TEST_T1, "5F--T1" },
                    { LINETYPE.TEST_T2, "5F--T2" }
                },
                filePath = @"\\aclfile2.advantech.corp\Group1\DF\PMC\生產日排程\TWM3 5F APS製程排程.xlsx",
                floorId = 1
            });

            settings.Add(new FileMappingSetting()
            {
                sheetLineTypeMappings = new Dictionary<LINETYPE, string>() {
                    { LINETYPE.PACKING, "5F--包裝" }
                },
                filePath = @"\\aclfile2.advantech.corp\Group1\DF\PMC\生產日排程\TWM3 5F APS製程排程.xlsx",
                floorId = 1
            });

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            initExcelParams();

            List<DateTime> dts = new List<DateTime>();

            //Get the next date data
            DateTime startDate = DateTime.Today;
            int captureDays = 7;

            //DateTime startDate = new DateTime(2021, 02, 22);
            //int captureDays = 2;
            

            if (startDate.Hour >= 20)
            {
                startDate = startDate.AddDays(startDate.DayOfWeek == DayOfWeek.Saturday ? 2 : 1);
            }

            dts.Add(startDate);
            DateTime dt = startDate.AddDays(1);

            for (int i = 1; i <= captureDays; i++)
            {
                if (dt.DayOfWeek == DayOfWeek.Sunday)
                {
                    dt = dt.AddDays(1);
                }
                dts.Add(dt);
                dt = dt.AddDays(1);
            }

            readAndSaveSchedule(dts);

            Application.Exit();
        }

        private DataSet readFile(string filePath)
        {
            string file = filePath;
            if (File.Exists(file))
            {
                var extension = Path.GetExtension(file).ToLower();
                Console.WriteLine("讀取檔案：" + file);
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //判斷格式套用讀取方法
                    IExcelDataReader reader = null;
                    if (extension == ".xls")
                    {
                        Console.WriteLine(" => XLS格式");
                        reader = ExcelReaderFactory.CreateBinaryReader(stream, new ExcelReaderConfiguration()
                        {
                            Password = "234",
                            FallbackEncoding = Encoding.GetEncoding("utf-8")
                        });
                    }
                    else if (extension == ".xlsx")
                    {
                        Console.WriteLine(" => XLSX格式");
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream, new ExcelReaderConfiguration()
                        {
                            Password = "234",
                            FallbackEncoding = Encoding.GetEncoding("utf-8")
                        });
                    }
                    else if (extension == ".csv")
                    {
                        Console.WriteLine(" => CSV格式");
                        reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                        {
                            FallbackEncoding = Encoding.GetEncoding("utf-8")
                        });
                    }
                    else if (extension == ".txt")
                    {
                        Console.WriteLine(" => Text(Tab Separated)格式");
                        reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                        {
                            FallbackEncoding = Encoding.GetEncoding("utf-8"),
                            AutodetectSeparators = new char[] { '\t' }
                        });
                    }

                    //沒有對應產生任何格式
                    if (reader == null)
                    {
                        return null;
                    }
                    Console.WriteLine(" => 轉換中");
                    using (reader)
                    {
                        DataSet ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            UseColumnDataType = false,
                            ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                            {
                                //設定讀取資料時是否忽略標題
                                UseHeaderRow = false
                            }
                        });
                        return ds;
                    }
                }
            }
            else
            {
                Console.WriteLine("檔案 " + file + " 不存在!");
                return null;
            }

        }

        private void readAndSaveSchedule(List<DateTime> dts)
        {
            foreach (FileMappingSetting setting in settings)
            {
                Dictionary<LINETYPE, string> sheetLineTypeMappings = setting.sheetLineTypeMappings;

                string filePath = setting.filePath;
                int floorId = setting.floorId;

                //For testing table.row["N"] is a number or not 
                int i = 0;

                //PMC remark field index(always in excel row 3, index -1)
                int remarkIndex = 2;

                DataSet ds = this.readFile(filePath);
                if (ds == null)
                {
                    return;
                }

                foreach (KeyValuePair<LINETYPE, string> mapping in sheetLineTypeMappings)
                {
                    LINETYPE lineType = mapping.Key;
                    string sheetName = mapping.Value;

                    //Get sheet
                    var table = ds.Tables[sheetName];

                    List<PrepareSchedule> dataInExcel = new List<PrepareSchedule>();

                    foreach (DateTime dt in dts)
                    {
                        Console.WriteLine("Process {0} {1} Data", lineType.ToString(), dt.ToString("yyyy/MM/dd"));

                        //Get date field match current date
                        int dateIndex = 0;
                        for (var col = 1; col < table.Columns.Count; col++)
                        {
                            var check_data = table.Rows[5][col];
                            if (dt.Equals(check_data))
                            {
                                dateIndex = col;
                                break;
                            }
                        }

                        string remark = table.Rows[remarkIndex][dateIndex].ToString();

                        //Save pmc remark at specific field
                        if (lineType != LINETYPE.PREASSY)
                        {
                            saveRemark(remark, (int)lineType, dt);
                        }

                        //把 DataSet 顯示出來
                        for (int row = 0; row < table.Rows.Count; row++)
                        {
                            if (row < 6)
                            {
                                continue;
                            }

                            string totalQtyField = table.Rows[row][4].ToString();
                            string scheduleQtyField = table.Rows[row][dateIndex].ToString();
                            var modelField = table.Rows[row][2];

                            if (int.TryParse(totalQtyField, out i) == true && Int32.Parse(totalQtyField) > 0 && int.TryParse(scheduleQtyField, out i) == true && Int32.Parse(scheduleQtyField) > 0 && !System.DBNull.Value.Equals(modelField))
                            {
                                var scheduleProcessField = table.Rows[row][1].ToString();

                                if (!System.DBNull.Value.Equals(modelField))
                                {
                                    Console.WriteLine("Type: {0} and ModelName: {1}", modelField.GetType(), modelField);

                                    string po = table.Rows[row][3].ToString().Trim();
                                    string modelName = table.Rows[row][2].ToString().Trim();
                                    int totalQty = Int32.Parse(totalQtyField);
                                    int scheduleQty = Int32.Parse(scheduleQtyField);

                                    string poRemark = table.Rows[row][14].ToString().Trim();

                                    decimal timeCost;
                                    int lineTypeId;

                                    if ((lineType == LINETYPE.PREASSY && scheduleProcessField.Contains("BASSY")) || (lineType == LINETYPE.ASSY && scheduleProcessField.Contains("AASSY")))
                                    {
                                        continue;
                                    }

                                    if (lineType == LINETYPE.PREASSY || lineType == LINETYPE.ASSY)
                                    {
                                        lineTypeId = scheduleProcessField.Contains("BASSY") ? (int)LINETYPE.ASSY : (int)LINETYPE.PREASSY;
                                    }
                                    else
                                    {
                                        lineTypeId = (int)lineType;
                                    }

                                    if (lineType == LINETYPE.PACKING)
                                    {
                                        var fitData = worktimes.FirstOrDefault(w => modelName.Equals(w.modelName));
                                        if (fitData == null)
                                        {
                                            Console.WriteLine("{0}, packingLeadTime is zero", modelName);
                                            continue;
                                        }
                                        timeCost = (fitData.packingLeadTime ?? 0) * scheduleQty;
                                    }
                                    else
                                    {
                                        timeCost = Decimal.Parse(table.Rows[row][dateIndex + 1].ToString());
                                    }


                                    dataInExcel.Add(new PrepareSchedule()
                                    {
                                        po = po,
                                        modelName = modelName,
                                        lineType_id = lineTypeId,
                                        totalQty = totalQty,
                                        scheduleQty = scheduleQty,
                                        timeCost = timeCost,
                                        floor_id = floorId,
                                        onboardDate = dt,
                                        priority = 0,
                                        undoneQty = 0,
                                        po_memo = "".Equals(poRemark) ? null : poRemark,
                                        createDate = DateTime.Today
                                    });

                                }
                            }

                        }

                    }

                    List<PrepareSchedule> dataInDb = db.PrepareSchedule.Where(
                        p => dts.Contains(p.onboardDate) &&
                        (p.lineType_id == (int)lineType) &&
                        p.floor_id == floorId)
                        .ToList();

                    //Compare data and add/remove
                    var newData = dataInExcel.Except(dataInDb);
                    var deletedData = dataInDb.Except(dataInExcel);

                    db.PrepareSchedule.AddRange(newData);
                    db.PrepareSchedule.RemoveRange(deletedData);
                    db.SaveChanges();

                    Console.WriteLine("FloorId {0} {1}, Data in sql: {2}, data in excel: {3}",
                            floorId, lineType.ToString(), dataInDb.Count(), dataInExcel.Count()
                        );

                    Console.WriteLine("FloorId {0} {1}, Data total add cnt: {2}, remove cnt: {3}",
                            floorId, lineType.ToString(), newData.Count(), deletedData.Count()
                        );

                }
            }
        }

        private void saveRemark(string remark, int lineType_id, DateTime dt)
        {
            PrepareScheduleRemark_PMC existData = db.PrepareScheduleRemark_PMC
                .Where(p => p.date == dt && p.lineType_id == lineType_id)
                .FirstOrDefault();

            if (!"".Equals(remark.Trim()))
            {
                if (existData == null)
                {
                    db.PrepareScheduleRemark_PMC.Add(new PrepareScheduleRemark_PMC()
                    {
                        date = dt,
                        pmc_remark = remark,
                        lineType_id = lineType_id
                    });
                }
                else
                {
                    existData.pmc_remark = remark;
                    db.Entry(existData).State = EntityState.Modified;
                }
                db.SaveChanges();
            }
        }
    }
}
