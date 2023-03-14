export var graphDataProduce = {
  xAxisLabel: 'Production',
  yAxisLabel: 'Tax%',
  legendTitle: 'History',
  domain: ['#7AA3E5', '#A8385D', '#A27EA8'],
  view: [420, 200],
  showLegend: true,
  showYAxisLabel: false,
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
      "name": "Citizen",
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
