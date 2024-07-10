import { PublicHashPipe } from './public-hash.pipe';

describe(('PublicHashPipe'), () => {
    const pipe = new PublicHashPipe();

    it('create an instance', () => {  
        expect(pipe).toBeTruthy();
    });

    it('convert public key', () => {    
        expect(pipe.transform('0xb197dC47fCbE7D7734B60fA87FD3b0BA0ACaf441')).toEqual('0xb197...f441');
    });

    it('No key should return ...', () => {        
        expect(pipe.transform('')).toBe('...');
    });

    it('Null key should return ...', () => {        
        expect(pipe.transform(null)).toBe('...');
    });
});
