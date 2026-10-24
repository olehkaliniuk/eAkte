using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace EAktePrototypProjektNewMvc.Services
{
    public class WordVorlagenService
    {

        /// <summary>
        /// Befüllt Content Controls (Tags) in Word-Dokumenten mit Daten.
        /// </summary>
        /// <param name="wordDocument"></param>
        /// <param name="tagValueList"></param>
        /// <param name="ignoreTagIfValueIsEmptyString">Befüllt das Content Control (Tag) nicht, wenn leerer value übergeben wurde.</param>
        /// <returns></returns>
        public byte[] FillContentControls(byte[] wordDocBytes, Dictionary<string, string> tagValueList, bool ignoreTagIfValueIsEmptyString)
        {
            using var memoryStream = new MemoryStream();
            memoryStream.Write(wordDocBytes, 0, wordDocBytes.Length);
            memoryStream.Position = 0;

            using (var wordDocument = WordprocessingDocument.Open(memoryStream, true))
            {
                var mainPart = wordDocument.MainDocumentPart ?? throw new InvalidOperationException("Kein MainDocumentPart im Dokument.");

                // Body des Dokuments befüllen.
                FillSdtElements(mainPart.Document?.Body?.Descendants<SdtElement>().ToList(), tagValueList, ignoreTagIfValueIsEmptyString);

                // Header und Footer befüllen.
                foreach (var headerPart in mainPart.HeaderParts)
                {
                    FillSdtElements(headerPart.Header?.Descendants<SdtElement>().ToList(), tagValueList, ignoreTagIfValueIsEmptyString); headerPart.Header?.Save();
                }

                // Footer befüllen.
                foreach (var footerPart in mainPart.FooterParts)
                {
                    FillSdtElements(footerPart.Footer?.Descendants<SdtElement>().ToList(), tagValueList, ignoreTagIfValueIsEmptyString); footerPart.Footer?.Save();
                }

                mainPart.Document.Save();
            } // Dispose schreibt das Paket in den Stream.

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Befüllt die übergebnen SdtElemente anhand der Key-Value Liste.
        /// </summary>
        /// <param name="sdtElements"></param>
        /// <param name="tagValueList"></param>
        /// <param name="ignoreTagIfValueIsEmptyString">Befüllt das Content Control (Tag) nicht, wenn leerer value übergeben wurde.</param>
        private void FillSdtElements(IEnumerable<SdtElement> sdtElements, Dictionary<string, string> tagValueList, bool ignoreTagIfValueIsEmptyString)
        {
            if (sdtElements == null)
            {
                return;
            }

            foreach (var sdt in sdtElements)
            {
                string tagName =
                    sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                if (string.IsNullOrWhiteSpace(tagName))
                {
                    continue;
                }

                string tagValue =
                    tagValueList.TryGetValue(tagName, out var val) ?
                    val :
                    null;

                // Null überspringen; leeren String ggf. je nach Flag ignorieren.
                bool isTagValueEmptyOrInvalid =
                    tagValue == null ||
                    (string.IsNullOrWhiteSpace(tagValue) &&
                    ignoreTagIfValueIsEmptyString);
                if (isTagValueEmptyOrInvalid)
                {
                    continue;
                }

                // 1) Content-Container ermitteln (Run, Block oder Cell).
                OpenXmlElement sdtContent =
                       (OpenXmlElement)sdt.GetFirstChild<SdtContentRun>()
                    ?? (OpenXmlElement)sdt.GetFirstChild<SdtContentBlock>()
                    ?? (OpenXmlElement)sdt.GetFirstChild<SdtContentCell>();

                if (sdtContent == null)
                {
                    continue;
                }

                // 2) "Template-Paragraph" + "Template-Run" ermitteln/erzeugen.
                DocumentFormat.OpenXml.Wordprocessing.Paragraph templatePara = null;
                Run templateRun = null;

                if (sdtContent is SdtContentRun scr)
                {
                    // Inline-CC -> erster Run innerhalb des sdtContent
                    templateRun = scr.Descendants<Run>().FirstOrDefault();
                    if (templateRun == null)
                    {
                        templateRun = scr.AppendChild(new Run());
                    }
                    templatePara =
                        templateRun.Ancestors<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().FirstOrDefault(); // kann null sein (bei reinem Run).
                }
                else
                {
                    // Block/Cell -> erster Paragraph und darin erster Run.
                    templatePara =
                        sdtContent.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().FirstOrDefault();
                    if (templatePara == null)
                    {
                        templatePara = sdtContent.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                    }

                    templateRun =
                        templatePara.Descendants<Run>().FirstOrDefault();
                    if (templateRun == null)
                    {
                        templateRun = templatePara.AppendChild(new Run());
                    }
                }

                // 3) Alte sichtbare Inhalte sicher entfernen,
                //    aber Run-/Paragraph-Props, Feldcodes, Zeichnungen usw. unangetastet lassen.
                //    -> Nur Text/Break/Tab innerhalb des ContentControls entfernen.
                foreach (var t in sdtContent.Descendants<Text>().ToList())
                {
                    t.Remove();
                }
                foreach (var br in sdtContent.Descendants<Break>().ToList())
                {
                    br.Remove();
                }
                foreach (var tab in sdtContent.Descendants<TabChar>().ToList())
                {
                    tab.Remove();
                }

                // 4) Sicherstellen, dass in einem Run geschrieben wird:
                //    Im Template-Run selbst nur Text/Break/Tab löschen (andere Kindelemente bleiben),
                //    damit rPr (Fett/Unterstrichen etc.) erhalten bleibt.
                foreach (var t in templateRun.Elements<Text>().ToList())
                {
                    t.Remove();
                }
                foreach (var br in templateRun.Elements<Break>().ToList())
                {
                    br.Remove();
                }
                foreach (var tab in templateRun.Elements<TabChar>().ToList())
                {
                    tab.Remove();
                }

                // 5) Neue Inhalte einfügen – Zeilen exakt als <w:br/> trennen.
                var lines =
                    tagValue.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

                bool first = true;
                foreach (var line in lines)
                {
                    if (!first)
                    {
                        templateRun.AppendChild(new Break()); // visueller Zeilenumbruch
                    }

                    templateRun.AppendChild(new Text(line) { Space = SpaceProcessingModeValues.Preserve });
                    first = false;
                }

                // 6) Leere zusätzliche Paragraphen entfernen,
                //    um „hängende“ Alt-Absätze zu vermeiden – nur wenn wirklich leer.
                if (sdtContent is SdtContentBlock || sdtContent is SdtContentCell)
                {
                    foreach (var p in sdtContent.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().ToList())
                    {
                        // Template-Paragraph behalten
                        if (templatePara != null && p == templatePara)
                        {
                            continue;
                        }

                        // Nur entfernen, wenn keine relevanten Inhalte enthalten sind.
                        bool hasVisibleContent =
                            p.Descendants<Text>().Any() ||
                            p.Descendants<Drawing>().Any() ||
                            p.Descendants<FieldChar>().Any() ||
                            p.Descendants<Hyperlink>().Any();

                        if (!hasVisibleContent)
                        {
                            p.Remove();
                        }
                    }
                }

                // 7) Leere zusätzliche Runs entfernen, die nach Schritt 3/4 keine sichtbaren Inhalte mehr haben.
                foreach (var r in sdtContent.Descendants<Run>().ToList())
                {
                    if (r == templateRun)
                    {
                        continue;
                    }

                    bool hasVisibleRunContent =
                        r.Descendants<Text>().Any() ||
                        r.Descendants<Break>().Any() ||
                        r.Descendants<TabChar>().Any() ||
                        r.Descendants<Drawing>().Any() ||
                        r.Descendants<FieldChar>().Any();

                    if (!hasVisibleRunContent)
                    {
                        r.Remove();
                    }
                }
            }
        }
    }
}
