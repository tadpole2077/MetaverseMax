import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'public-hash-pipe'
})

export class PublicHashPipe implements PipeTransform {

    transform(walletKey: string | null): string {

        walletKey = walletKey ?? '';

        return walletKey.substring(0, 6) + '...' + walletKey.substring(walletKey.length - 4, walletKey.length);

    }
}
