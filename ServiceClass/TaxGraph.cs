namespace MetaverseMax.ServiceClass
{
    public class TaxGraph
    {
        private DistrictWeb district;
        private DistrictWeb districtHistory;
        private string currentMonth;
        private string lastMonth;

        public TaxGraph(DistrictWeb districtWeb, DistrictWeb districtWebHistory)
        {
            district = districtWeb;
            districtHistory = districtWebHistory;

            currentMonth = districtWeb.last_update.ToString("MMM");
            lastMonth = districtWebHistory.last_update.ToString("MMM");
        }

        public NgxChart Construct()
        {
            NgxChart ngxChart = new()
            {

                domain = new string[] { "#5AA454", "#C7B42C", "#AAAAAA" },

                x_axis_label = "Construction",

                y_axis_label = "Tax%",

                view = new int[] { 430, 210 },

                show_legend = false,

                show_yaxis_label = true
            };


            ngxChart.graphColumns = new NGXGraphColumns[5]
            {
                new NGXGraphColumns(){
                    name = "Energy",
                    series = new NGXChartSeries[2] {
                        new NGXChartSeries(){ name = lastMonth, value = districtHistory.construction_energy_tax },
                        new NGXChartSeries(){ name = currentMonth, value = district.construction_energy_tax }
                    }
                },
                new NGXGraphColumns()
                {
                    name = "Ind+Prod",
                    series = new NGXChartSeries[2] {
                        new NGXChartSeries(){ name = lastMonth, value = districtHistory.construction_industry_production_tax },
                        new NGXChartSeries(){ name = currentMonth, value = district.construction_industry_production_tax }
                    }
                },
                new NGXGraphColumns()
                {
                    name = "Off+Com",
                    series = new NGXChartSeries[2] {
                        new NGXChartSeries(){ name = lastMonth, value = districtHistory.construction_commercial_tax },
                        new NGXChartSeries(){ name = currentMonth, value = district.construction_commercial_tax }
                    }
                },
                new NGXGraphColumns()
                {
                    name = "Muni.",
                    series = new NGXChartSeries[2] {
                        new NGXChartSeries(){ name = lastMonth, value = districtHistory.construction_municipal_tax },
                        new NGXChartSeries(){ name = currentMonth, value = district.construction_municipal_tax }
                    }
                },
                new NGXGraphColumns()
                {
                    name = "Resid.",
                    series = new NGXChartSeries[2] {
                        new NGXChartSeries(){ name = lastMonth, value = districtHistory.construction_residential_tax },
                        new NGXChartSeries(){ name = currentMonth, value = district.construction_residential_tax }
                    }
                }
            };

            return ngxChart;
        }

        public NgxChart Produce()
        {

            NgxChart ngxChart = new()
            {

                domain = new string[] { "#7AA3E5", "#A8385D", "#A27EA8" },

                x_axis_label = "Production",

                y_axis_label = "Tax%",

                legend_title = "History",

                view = new int[] { 420, 210 },

                show_legend = true,

                show_yaxis_label = false
            };


            ngxChart.graphColumns = new NGXGraphColumns[4]
            {
                new NGXGraphColumns(){
                    name = "Energy",
                    series = new NGXChartSeries[2] {
                        new NGXChartSeries(){ name = lastMonth, value = districtHistory.energy_tax },
                        new NGXChartSeries(){ name = currentMonth, value = district.energy_tax }
                    }
                },
                new NGXGraphColumns()
                {
                    name = "Ind+Prod",
                    series = new NGXChartSeries[2] {
                        new NGXChartSeries(){ name = lastMonth, value = districtHistory.production_tax },
                        new NGXChartSeries(){ name = currentMonth, value = district.production_tax }
                    }
                },
                new NGXGraphColumns()
                {
                    name = "Off+Com",
                    series = new NGXChartSeries[2] {
                        new NGXChartSeries(){ name = lastMonth, value = districtHistory.commercial_tax },
                        new NGXChartSeries(){ name = currentMonth, value = district.commercial_tax }
                    }
                },
                new NGXGraphColumns()
                {
                    name = "Citizen",
                    series = new NGXChartSeries[2] {
                        new NGXChartSeries(){ name = lastMonth, value = districtHistory.citizen_tax},
                        new NGXChartSeries(){ name = currentMonth, value = district.citizen_tax }
                    }
                }
            };
            return ngxChart;
        }
    }
}
