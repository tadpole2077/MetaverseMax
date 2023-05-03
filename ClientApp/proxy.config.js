const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:17190';

const PROXY_CONFIG = [
  {
    context: [
      "/AssetHistory",
      "/District",
      "/DistrictFund",
      "/OwnerData",
      "/OwnerSummary",
      "/Plot",
      "/api",
      "/images",

    ],
    target: target,
    secure: false,
    headers: {
      Connection: 'Keep-Alive'
    },
    loglevel: "debug"     // experimental - can remove - added to expose more debug data during proxy setup/testing - did not observe any additional debug data
  }
]

module.exports = PROXY_CONFIG;

// Good ref sites
// https://medium.com/ngconf/how-to-proxy-http-requests-in-angular-f873183880a4
// http-proxy events
//    Test template logs (not shown in MetaverseMax) >>  [webpack-dev-server] [HPM] Subscribed to http-proxy events: [ 'error', 'close' ]
//    Node Module : https://www.npmjs.com/package/http-proxy-middleware : http-proxy-middleware@2.0.6   (same version as test template)
//
// http-proxy-middleware Node Module
//    Used by AspNetCore.SpaProxy with this proxy.config.js file
//    On serve (development),  logs multiple info messages:
//       [webpack-dev-server] [HPM] Proxy created: /AssetHistory,/District,/DistrictFund,/OwnerData,/OwnerSummary,/Plot  -> http://localhost:17190
//       [webpack-dev-server] [HPM] Subscribed to http - proxy events: ['error', 'close']
//
// Configuration to use:
//    File: angular.json >> projects >> MetaverseMax >> architect >> serve >> configurations >> development
//    Setting: "proxyConfig": "proxy.config.js"
