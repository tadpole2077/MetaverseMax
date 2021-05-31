export var graphDataConstruct = {
  xAxisLabel: 'Construction',
  yAxisLabel: 'Tax%',
  legendTitle: 'History',
  domain: ['#5AA454', '#C7B42C', '#AAAAAA'],
  view: [420, 200],
  showLegend: false,
  showYAxisLabel: true,
  graphColumns: [
    {
      "name": "Energy",
      "series": [
        {
          "name": "Apr",
          "value": 5          
        },
        {
          "name": "May",
          "value": 10
        }
      ]
    },

    {
      "name": "Ind+Prod",
      "series": [
        {
          "name": "Apr",
          "value": 5
        },
        {
          "name": "May",
          "value": 10
        }
      ]
    },

    {
      "name": "Off+Com",
      "series": [
        {
          "name": "Apr",
          "value": 5
        },
        {
          "name": "May",
          "value": 5
        }
      ]
    },

    {
      "name": "Munic.",
      "series": [
        {
          "name": "Apr",
          "value": 10
        },
        {
          "name": "May",
          "value": 5
        }
      ]
    },
    {
      "name": "Resid.",
      "series": [
        {
          "name": "Apr",
          "value": 50
        },
        {
          "name": "May",
          "value": 30
        }
      ]
    }
  ]
};
