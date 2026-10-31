"""Patch the retained report package while preserving all unrelated XML parts.

Usage: bundled-python build_report.py --output E:/.../report.docx [--final]
The --final gate rejects unverified evidence placeholders.
"""
from pathlib import Path
from copy import deepcopy
from zipfile import ZipFile, ZIP_DEFLATED
from datetime import datetime, timezone
import argparse, hashlib, json, re
from lxml import etree
from PIL import Image
from report_content import PARAGRAPHS, TABLE

ROOT = Path(__file__).resolve().parent
REFERENCE_HASH = '65e127f24136e0157ed961c81bde760b6c7368e1b30dc7da191a05a498b4d456'
NS = {'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main',
      'wp':'http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing',
      'a':'http://schemas.openxmlformats.org/drawingml/2006/main'}
def tag(name):
    prefix,local=name.split(':');return '{'+NS[prefix]+'}'+local
def xml(tree):return etree.tostring(tree,xml_declaration=True,encoding='UTF-8',standalone=True)

def replace_text(paragraph, text):
    previous=paragraph.find('w:r/w:rPr',NS)
    for child in list(paragraph):
        if child.tag != tag('w:pPr'):paragraph.remove(child)
    run=etree.SubElement(paragraph,tag('w:r'))
    if previous is not None:run.append(deepcopy(previous))
    for index,line in enumerate(text.split('\n')):
        if index:etree.SubElement(run,tag('w:br'))
        node=etree.SubElement(run,tag('w:t'));node.set('{http://www.w3.org/XML/1998/namespace}space','preserve');node.text=line

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);parser.add_argument('--final',action='store_true')
    args=parser.parse_args();output=Path(args.output).resolve()
    if not str(output).lower().startswith('e:'):raise ValueError('Output must remain on E:')
    source=ROOT/'reference-template.docx'
    assert hashlib.sha256(source.read_bytes()).hexdigest()==REFERENCE_HASH,'Reference template changed'
    evidence=json.loads((ROOT/'evidence.json').read_text(encoding='utf-8'))
    with ZipFile(source) as z:parts={name:z.read(name) for name in z.namelist()}
    original=dict(parts);document=etree.fromstring(parts['word/document.xml'])
    body=document.find('w:body',NS);paragraphs=body.findall('w:p',NS)
    assert len(paragraphs)==117,'Template slot map changed'
    for index,text in PARAGRAPHS.items():
        for key,value in evidence.items():text=text.replace('{'+key+'}',value)
        replace_text(paragraphs[index],text)
    table=body.find('w:tbl',NS)
    for row,values in zip(table.findall('w:tr',NS),TABLE):
        for cell,text in zip(row.findall('w:tc',NS),values):
            cell_paragraphs=cell.findall('w:p',NS)
            replace_text(cell_paragraphs[0],text)
            for extra in cell_paragraphs[1:]:cell.remove(extra)
    figures=[(13,'architecture-dependencies.png','image4.png','Direct references between four application projects; tests are separate.'),
             (22,'architecture-mvc.png','image5.png','HTTP request through controller and service to database; Razor returns HTML.'),
             (26,'architecture-sequence.png','image6.png','Successful query, database failure and later recovery, and order transaction safety.')]
    for index,filename,media,alt in figures:
        picture=ROOT/filename
        with Image.open(picture) as im:width,height=im.size
        cx=round(6*914400);cy=round(cx*height/width)
        for node in paragraphs[index].findall('.//wp:extent',NS)+paragraphs[index].findall('.//a:ext',NS):
            node.set('cx',str(cx));node.set('cy',str(cy))
        for node in paragraphs[index].findall('.//wp:docPr',NS):node.set('descr',alt);node.set('title',filename)
        parts['word/media/'+media]=picture.read_bytes()
    parts['word/document.xml']=xml(document)
    settings=etree.fromstring(parts['word/settings.xml'])
    update=settings.find('w:updateFields',NS)
    if update is None:update=etree.SubElement(settings,tag('w:updateFields'))
    update.set(tag('w:val'),'true');parts['word/settings.xml']=xml(settings)
    core=etree.fromstring(parts['docProps/core.xml'])
    updates={'{http://purl.org/dc/elements/1.1/}title':'在线商店系统架构与项目反思',
             '{http://purl.org/dc/elements/1.1/}subject':'OnlineStore baseline 4bfdc48 and quality improvements 2026-10-29',
             '{http://purl.org/dc/elements/1.1/}description':'Architecture, five quality attributes, evidence and reflection.',
             '{http://schemas.openxmlformats.org/package/2006/metadata/core-properties}lastModifiedBy':'',
             '{http://purl.org/dc/terms/}modified':datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')}
    for name,value in updates.items():
        node=core.find(name)
        if node is None:node=etree.SubElement(core,name)
        node.text=value
    parts['docProps/core.xml']=xml(core)
    full_text='\n'.join(document.xpath('//w:t/text()',namespaces=NS))
    for obsolete in ['8.0.8','4dd3827','138 项','尚无全局 IP 限流','可逆保护账号和密码','代码未要求先购买商品']:
        assert obsolete not in full_text,'Obsolete claim remains: '+obsolete
    assert not re.search(r'\{[A-Z_]+\}',full_text),'Unresolved template key'
    if args.final:assert '待核验' not in full_text,'Final report still has unverified evidence'
    allowed={'word/document.xml','word/settings.xml','docProps/core.xml','word/media/image4.png','word/media/image5.png','word/media/image6.png'}
    changed=[name for name in original if original[name]!=parts[name]]
    assert set(changed)<=allowed,'Unexpected template package change'
    output.parent.mkdir(parents=True,exist_ok=True)
    with ZipFile(output,'w',ZIP_DEFLATED) as z:
        for name,data in parts.items():z.writestr(name,data)
    (ROOT/'report-text.txt').write_text(full_text,encoding='utf-8')
    qa={'output':str(output),'sha256':hashlib.sha256(output.read_bytes()).hexdigest(),
        'reference_sha256':REFERENCE_HASH,'preserve_only_parts_unchanged':True,'changed_parts':changed,
        'embedded_images':len(figures),'cjk_characters':len(re.findall(r'[\u4e00-\u9fff]',full_text)),
        'ascii_word_tokens':len(re.findall(r'\b[A-Za-z][A-Za-z0-9]*\b',full_text)),
        'contains_unverified_placeholders':'待核验' in full_text,'render_visual_review':'required separately'}
    (ROOT/'report-structure.json').write_text(json.dumps(qa,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(qa,ensure_ascii=False,indent=2))

if __name__=='__main__':main()
