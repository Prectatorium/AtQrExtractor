# License

## MIT License

Copyright (c) 2025 AT QR Code Extractor Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

## Third-Party Dependencies

This project incorporates several third-party open-source libraries. Each library is governed by its respective license, which remains in effect alongside this project. The following sections acknowledge these dependencies and their licensing terms.

### QrCodeNet (QRCoder)

This project uses QrCodeNet for QR code detection capabilities. QrCodeNet is licensed under the MIT License. The MIT License terms for QrCodeNet are compatible with and do not conflict with the MIT License governing this project.

Copyright (c) 2016-2023 Raffael Herrmann

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

### PdfLibNet

This project uses PdfLibNet for PDF document rendering and processing. PdfLibNet is licensed under the MIT License.

### Serilog

This project uses Serilog for structured logging capabilities. Serilog is licensed under the Apache License 2.0.

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

<http://www.apache.org/licenses/LICENSE-2.0>

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.

### System.CommandLine

This project uses System.CommandLine for command-line interface capabilities. System.CommandLine is licensed under the MIT License.

Copyright (c) .NET Foundation and Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

## EPPlus Licensing Notice

This project uses EPPlus for Excel report generation. EPPlus has specific licensing requirements that differ from the permissive MIT License used by this project.

### Non-Commercial Use

By default, this project configures EPPlus for non-commercial personal use through the following license setting:

```csharp
ExcelPackage.License.SetNonCommercialPersonal("Dude");
```

This configuration permits use of EPPlus for personal, non-commercial purposes without requiring a commercial license. If you are using this software for personal purposes only, no additional licensing action is required.

### Commercial Use

Organizations using this software for commercial purposes must obtain an appropriate EPPlus license from the EPPlus maintainers. Commercial use includes, but is not limited to, using this software in business operations, integrating it into commercial products or services, or processing documents on behalf of commercial clients.

EPPlus offers various licensing options including single-developer licenses, team licenses, and enterprise licenses. Pricing and terms are available on the official EPPlus website. Using EPPlus without a valid commercial license when required constitutes a violation of EPPlus licensing terms.

For questions about EPPlus licensing, please contact the EPPlus maintainers directly through their official channels. The AT QR Code Extractor project is not affiliated with EPPlus Software AB and cannot provide guidance on licensing matters beyond the basic information provided here.

### Alternative Excel Libraries

If your use case requires commercial Excel generation capabilities and you prefer to avoid EPPlus licensing considerations, consider substituting EPPlus with an alternative library that provides suitable commercial licensing terms. Several open-source and commercial alternatives exist, including ClosedXML (MIT License), NPOI (Apache License 2.0), and Aspose.Cells (commercial license).

---

## Disclaimer

This software is provided "as is" without warranty of any kind, either express or implied. The authors and contributors of this project shall not be held liable for any damages arising from the use of this software, including but not limited to direct, indirect, incidental, special, consequential, or punitive damages.

The use of third-party libraries, including but not limited to those listed in this document, is subject to their respective licenses. This project does not modify, extend, or restrict any third-party license terms. Users of this software are responsible for ensuring compliance with all applicable licenses.

---

## Attribution

This project was inspired by the needs of businesses and developers working with Portuguese fiscal documents subject to Portaria n.º 195/2020 requirements. The specification defines standards for QR codes on invoices and other fiscal documents in Portugal, administered by the Autoridade Tributária e Aduaneira (AT).

QR code detection functionality is provided through QRCoder, a well-established .NET library for QR code generation and recognition. PDF processing capabilities rely on PdfLibNet for document rendering. Excel report generation uses EPPlus for professional-grade spreadsheet output. Structured logging is implemented through Serilog, and the command-line interface is built on System.CommandLine.

We acknowledge and thank the maintainers and contributors of these open-source projects for their work, which enables the creation of tools like this one.
