import { Injectable } from '@angular/core';
import { PRODUCT_NAME, PRODUCT, PRODUCT_IMG, FACTORY_PRODUCT_IMG } from '../common/enum'

@Injectable()
export class MapData {

  getProductName(productId: number) {
    return PRODUCT_NAME[productId];
  }

  getImageName(productId: number) {

    let productImg: string;

    switch (productId) {
      case PRODUCT.WOOD:
        productImg = PRODUCT_IMG.WOOD;
        break;
      case PRODUCT.SAND:
        productImg = PRODUCT_IMG.SAND;
        break;
      case PRODUCT.METAL:
        productImg = PRODUCT_IMG.METAL;
        break;
      case PRODUCT.STONE:
        productImg = PRODUCT_IMG.STONE;
        break;
      case PRODUCT.BRICK:
        productImg = PRODUCT_IMG.BRICK;
        break;
      case PRODUCT.GLASS:
        productImg = PRODUCT_IMG.GLASS;
        break;
      case PRODUCT.CONCRETE:
        productImg = PRODUCT_IMG.CONCRETE;
        break;
      case PRODUCT.PLASTIC:
        productImg = PRODUCT_IMG.PLASTIC;
        break
      case PRODUCT.STEEL:
        productImg = PRODUCT_IMG.STEEL;
        break;
      case PRODUCT.PAPER:
        productImg = PRODUCT_IMG.PAPER;
        break;
      case PRODUCT.COMPOSITE:
        productImg = PRODUCT_IMG.COMPOSITE;
        break;
      case PRODUCT.GLUE:
        productImg = PRODUCT_IMG.GLUE;
        break;
      case PRODUCT.MIXES:
        productImg = PRODUCT_IMG.MIXES;
        break;
      case PRODUCT.ENERGY:
        productImg = PRODUCT_IMG.ENERGY;
        break;
      case PRODUCT.WATER:
        productImg = PRODUCT_IMG.WATER;
        break;
      case PRODUCT.FACTORY_PRODUCT:
        productImg = FACTORY_PRODUCT_IMG[7];
        break;
      default:
        productImg = "";
        break;
    }

    return productImg;
  }
}
