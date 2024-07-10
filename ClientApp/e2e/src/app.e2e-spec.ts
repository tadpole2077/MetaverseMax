import { browser, element, by } from 'protractor'

describe ('MetaverseMax E2E', ()=>{

  beforeEach(async ()=>{
    // Code to run before each test
    await browser.get('/bnb');  // relative path to home page
  });
  
  it('should have header', async ()=>{
    const header = by.css('h2');

    const text = await element(header).getText();
    expect(text).toBe('MetaverseMax!');    
  });

  it('should have at least 3 action panals visible', async ()=>{
    const items = by.css('.action');

    const elementList = element.all(items);
    expect(await elementList.count()).toBeGreaterThan(2);    
  });

});
