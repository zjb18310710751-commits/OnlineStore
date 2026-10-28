const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');
const assert = require('node:assert/strict');

function setup(stockTemplate = 'Stock: {0}') {
    const skus = [
        { Id: 1, Color: 'Red', Spec: 'S', Price: 10, Stock: 5, ImageUrl: null },
        { Id: 2, Color: 'Blue', Spec: 'M', Price: 20, Stock: 2, ImageUrl: null },
        { Id: 3, Color: 'Red', Spec: 'L', Price: 30, Stock: 0, ImageUrl: null }
    ];
    const makeElement = (value = '') => {
        let inputValue = String(value);
        const classes = new Set();
        const handlers = {};
        return {
            get value() { return inputValue; },
            set value(v) { inputValue = String(v); },
            textContent: '', disabled: false, max: '10', dataset: {},
            classList: { add: name => classes.add(name), remove: name => classes.delete(name), contains: name => classes.has(name) },
            addEventListener(name, action) { handlers[name] = action; },
            click() { if (handlers.click) handlers.click(); }
        };
    };
    const chips = [['color','Red'],['color','Blue'],['spec','S'],['spec','M'],['spec','L']].map(([dim,value])=>Object.assign(makeElement(),{dataset:{dim,value}}));
    const submitButtons = [makeElement(),makeElement()];
    const elements = Object.fromEntries(['priceText','stockText','originalPriceBox','skuIdInput','qty','addToCartForm'].map(id=>[id,makeElement()]));
    elements.qty.value = '1';
    elements.addToCartForm.querySelectorAll = selector => selector.includes('submit') ? submitButtons : [];
    const query = selector => {
        if (selector.includes('submit')) return submitButtons;
        const dim = selector.match(/data-dim="([^"]+)"/);
        return selector.startsWith('.sku-chip') ? chips.filter(chip=>!dim||chip.dataset.dim===dim[1]) : [];
    };
    const document = { getElementById:id=>elements[id]||null, querySelectorAll:query, querySelector:selector=>query(selector)[0]||null };
    const source = fs.readFileSync(path.resolve(__dirname,'../../OnlineStore/Views/Products/Details.cshtml'),'utf8');
    const script = source.match(/<script>([\s\S]*?)<\/script>/)[1].replace('@Html.Raw(skusJson)',JSON.stringify(skus)).replace('@Html.Raw(System.Text.Json.JsonSerializer.Serialize(Loc["StockLeft"].Value))',JSON.stringify(stockTemplate));
    vm.runInNewContext(script,{document,setMainImage(){}});
    return {elements,submitButtons,select:(dim,value)=>chips.find(chip=>chip.dataset.dim===dim&&chip.dataset.value===value).click()};
}

test('missing SKU combination cannot submit the previously selected variant',()=>{
    const view=setup();
    assert.equal(view.elements.skuIdInput.value,'1');
    view.select('color','Blue');
    assert.equal(view.elements.skuIdInput.value,'');
    assert.ok(view.submitButtons.every(button=>button.disabled));
    assert.equal(view.elements.priceText.textContent,'—');
    view.select('spec','M');
    assert.equal(view.elements.skuIdInput.value,'2');
    assert.ok(view.submitButtons.every(button=>!button.disabled));
    assert.equal(view.elements.priceText.textContent,'20.00');
});

test('selecting an unavailable SKU disables both purchasing actions',()=>{
    const view=setup();
    view.select('spec','L');
    assert.equal(view.elements.skuIdInput.value,'3');
    assert.ok(view.submitButtons.every(button=>button.disabled));
});

test('switching to a SKU with lower stock keeps quantity within its available stock',()=>{
    const view=setup();
    view.elements.qty.value='5';
    view.select('color','Blue');
    view.select('spec','M');
    assert.equal(view.elements.qty.value,'2');
});

test('localized stock text preserves Chinese and quotes while updating the quantity',()=>{
    const view=setup('库存 {0} 件 · "精选"');
    assert.equal(view.elements.stockText.textContent,'库存 5 件 · "精选"');
    view.select('color','Blue');
    assert.equal(view.elements.stockText.textContent,'库存 0 件 · "精选"');
    view.select('spec','M');
    assert.equal(view.elements.stockText.textContent,'库存 2 件 · "精选"');
});
