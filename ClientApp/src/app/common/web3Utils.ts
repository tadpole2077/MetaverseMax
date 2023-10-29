import { padLeft, toHex, toBigInt, toWei, ChunkResponseParser, Eip1193Provider, EventEmitter, SocketProvider, Web3DeferredPromise, asciiToHex, bytesToHex, bytesToUint8Array, checkAddressCheckSum, compareBlockNumbers, convert, convertScalarValue, encodePacked, ethUnitMap, format, fromAscii, fromDecimal, fromTwosComplement, fromUtf8, fromWei, getStorageSlotNumForLongString, hexToAscii, hexToBytes, hexToNumber, hexToNumberString, hexToString, hexToUtf8, isAddress, isBatchRequest, isBatchResponse, isBloom, isContractAddressInBloom, isDataFormat, isHex, isHexStrict, isInBloom, isNullish, isPromise, isResponseRpcError, isResponseWithError, isResponseWithNotification, isResponseWithResult, isSubscriptionResult, isTopic, isTopicInBloom, isUserEthereumAddressInBloom, isValidResponse, jsonRpc, keccak256, keccak256Wrapper, leftPad, mergeDeep, numberToHex, padRight, pollTillDefined, pollTillDefinedAndReturnIntervalId, processSolidityEncodePackedArgs, randomBytes, randomHex, rejectIfConditionAtInterval, rejectIfTimeout, rightPad, setRequestIdStart, sha3, sha3Raw, soliditySha3, soliditySha3Raw, stringToHex, toAscii, toBatchPayload, toBool, toChecksumAddress, toDecimal, toNumber, toPayload, toTwosComplement, toUtf8, uint8ArrayConcat, uint8ArrayEquals, utf8ToBytes, utf8ToHex, uuidV4, validateResponse, waitWithTimeout } from 'web3-utils';

// Wrapper class to convert web-utils JS library to a typescript class
// Purpose of which is to allow provide access to web-utils using a class container, avoid namespace issues with dup named functions
// Web3 Optimisation >> Tree shaking >> https://docs.web3js.org/guides/advanced/web3_tree_shaking_support_guide/
export class Web3Utils {

  padLeft: typeof padLeft = padLeft;
  toHex: typeof toHex = toHex;
  toBigInt: typeof toBigInt = toBigInt;
  toWei: typeof toWei = toWei;
  
}
