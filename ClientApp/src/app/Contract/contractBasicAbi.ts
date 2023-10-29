const testBasic_abi = [
   {
      "inputs": [],
      "name": "read",
      "outputs": [
        {
          "internalType": "uint256",
          "name": "",
          "type": "uint256"
        }
      ],
      "stateMutability": "view",
      "type": "function"
    },
    {
      "inputs": [
        {
          "internalType": "uint256",
          "name": "newValue",
          "type": "uint256"
        }
      ],
      "name": "write",
      "outputs": [],
      "stateMutability": "nonpayable",
      "type": "function"
    }
] as const;


export {
  testBasic_abi
};
