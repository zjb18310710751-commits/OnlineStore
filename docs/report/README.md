# Rebuild the architecture report

The editable report sources are `report_content.py` and `evidence.json`. `build_report.py` patches the retained `reference-template.docx` package, keeping its styles, geometry, footer, relationships and other unrelated parts unchanged. `build_diagrams.py` rebuilds the three diagrams with Pillow; the Windows Arial font files are read from the existing system installation. All generated files are placed on E:.

Use the Python runtime returned by the Codex workspace dependency loader, with Pillow and lxml available. Run from this directory:

```powershell
& $python .\build_diagrams.py
& $python .\build_report.py --output 'E:/Daily/team work/系统架构与项目反思-最终版.docx' --final
```

`$python` above is the absolute path of that bundled interpreter. The `--final` option refuses unresolved evidence placeholders. The script also rejects obsolete version/security claims, checks the retained template hash, verifies that only intended package parts changed, and emits `report-structure.json` beside these sources. It preserves the personal contribution fields as intentional blanks.

The source template is the previously WPS-reviewed report layout, retained unchanged to make formatting reproducible; its historical text is **not** current implementation evidence. Current prose replaces the mapped slots, and the three embedded diagrams are replaced with the new PNGs. `report-text.txt` is a generated text audit, not a separate final report.

Render the rebuilt DOCX and inspect every page before delivery. The packaged `render_docx.py` was attempted on this host but no bundled `soffice.exe` was available. WPS COM was unregistered and Word COM could not start, so the delivery workflow uses WPS GUI PDF export and a separate all-page visual check. Full-resolution page PNGs remain QA intermediates under `E:/DMU-Learning/.tmp/quality-uplift-20261029/report/`.

The final WPS export is retained as `系统架构与项目反思-排版预览.pdf`, with a 14-page contact sheet in `contact-sheet.png`. Every page was visually inspected at 150 dpi; diagrams are on pages 2–4 and the complete comparison table is on page 10. `layout-review.json` records the exact DOCX/PDF hashes, page scope, current evidence checks and review limitations. A new edit or export requires another visual review.

The report is Chinese with necessary English terminology. The structure JSON counts CJK characters and ASCII word tokens separately; neither number is presented as an equivalent English word count. Refer to the actual course submission requirements for the final word allowance and required language.
