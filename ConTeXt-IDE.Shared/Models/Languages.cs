﻿using CodeEditorControl_WinUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConTeXt_IDE.Shared.Models
{
    public static class FileLanguages
    {
        public static List<Language> LanguageList = new()
        {
            new("ConTeXt")
            {
                RegexTokens = new()
                {
                    { Token.Math, /*language=regex*/ @"\$.*?\$" },
                    { Token.Key, /*language=regex*/ @"(\w+?\s*?)(=)" },
                    { Token.Symbol, /*language=regex*/ @"[:=,.!?&+\-*\/\^~#;]" },
                    { Token.Command, /*language=regex*/ @"\\.+?\b" },
                    { Token.Style, /*language=regex*/ @"\\(tf|bf|it|sl|bi|bs|sc)(x|xx|[a-e])?\b|(\\tt|\\ss|\\rm)\b" },
                    { Token.Array, /*language=regex*/ @"\\(b|e)(T)(C|Ds?|H|N|Rs?|X|Y)\b|(\\\\|\\AR|\\DR|\\DC|\\DL|\\NI|\\NR|\\NC|\\HL|\\VL|\\FR|\\MR|\\LR|\\SR|\\TB|\\NB|\\NN|\\FL|\\ML|\\LL|\\TL|\\BL)\b" },
                    { Token.Environment, /*language=regex*/ @"\\(start|stop).+?\b" },
                    { Token.Reference, /*language=regex*/ @"(\b|#*?)\w+?:\#*?\w+?\b|\\ref\b" },
                    { Token.Comment, /*language=regex*/ @"\%.*" },
                    { Token.Bracket, /*language=regex*/ @"(?<!\\)(\[|\]|\(|\)|\{|\})" },
                },
                WordTokens = new()
                {
                    { Token.Primitive, new string[] { "\\year", "\\xtokspre", "\\xtoksapp", "\\xspaceskip", "\\xleaders", "\\xdef", "\\write", "\\wordboundary", "\\widowpenalty", "\\widowpenalties", "\\wd", "\\vtop", "\\vss", "\\vsplit", "\\vskip", "\\vsize", "\\vrule", "\\vpack", "\\voffset", "\\vfuzz", "\\vfilneg", "\\vfill", "\\vfil", "\\vcenter", "\\vbox", "\\vbadness", "\\valign", "\\vadjust", "\\useimageresource", "\\useboxresource", "\\uppercase", "\\unvcopy", "\\unvbox", "\\unskip", "\\unpenalty", "\\unless", "\\unkern", "\\uniformdeviate", "\\unhcopy", "\\unhbox", "\\underline", "\\uchyph", "\\uccode", "\\tracingstats", "\\tracingscantokens", "\\tracingrestores", "\\tracingparagraphs", "\\tracingpages", "\\tracingoutput", "\\tracingonline", "\\tracingnesting", "\\tracingmacros", "\\tracinglostchars", "\\tracingifs", "\\tracinggroups", "\\tracingfonts", "\\tracingcommands", "\\tracingassigns", "\\tpack", "\\topskip", "\\topmarks", "\\topmark", "\\tolerance", "\\tokspre", "\\toksdef", "\\toksapp", "\\toks", "\\time", "\\thinmuskip", "\\thickmuskip", "\\the", "\\textstyle", "\\textfont", "\\textdirection", "\\textdir", "\\tagcode", "\\tabskip", "\\synctex", "\\suppressprimitiveerror", "\\suppressoutererror", "\\suppressmathparerror", "\\suppresslongerror", "\\suppressifcsnameerror", "\\suppressfontnotfounderror", "\\string", "\\splittopskip", "\\splitmaxdepth", "\\splitfirstmarks", "\\splitfirstmark", "\\splitdiscards", "\\splitbotmarks", "\\splitbotmark", "\\special", "\\span", "\\spaceskip", "\\spacefactor", "\\skipdef", "\\skip", "\\skewchar", "\\showtokens", "\\showthe", "\\showlists", "\\showifs", "\\showgroups", "\\showboxdepth", "\\showboxbreadth", "\\showbox", "\\show", "\\shipout", "\\shapemode", "\\sfcode", "\\setrandomseed", "\\setlanguage", "\\setfontid", "\\setbox", "\\scrollmode", "\\scriptstyle", "\\scriptspace", "\\scriptscriptstyle", "\\scriptscriptfont", "\\scriptfont", "\\scantokens", "\\scantextokens", "\\savingvdiscards", "\\savinghyphcodes", "\\savepos", "\\saveimageresource", "\\savecatcodetable", "\\saveboxresource", "\\rpcode", "\\romannumeral", "\\rightskip", "\\rightmarginkern", "\\righthyphenmin", "\\rightghost", "\\right", "\\relpenalty", "\\relax", "\\readline", "\\read", "\\randomseed", "\\raise", "\\radical", "\\quitvmode", "\\pxdimen", "\\protrusionboundary", "\\protrudechars", "\\primitive", "\\prevgraf", "\\prevdepth", "\\pretolerance", "\\prerelpenalty", "\\prehyphenchar", "\\preexhyphenchar", "\\predisplaysize", "\\predisplaypenalty", "\\predisplaygapfactor", "\\predisplaydirection", "\\prebinoppenalty", "\\posthyphenchar", "\\postexhyphenchar", "\\postdisplaypenalty", "\\penalty", "\\pdfximage", "\\pdfxformresources", "\\pdfxformname", "\\pdfxformmargin", "\\pdfxformattr", "\\pdfxform", "\\pdfvorigin", "\\pdfvariable", "\\pdfuniqueresname", "\\pdfuniformdeviate", "\\pdftrailerid", "\\pdftrailer", "\\pdftracingfonts", "\\pdfthreadmargin", "\\pdfthread", "\\pdftexversion", "\\pdftexrevision", "\\pdftexbanner", "\\pdfsuppressptexinfo", "\\pdfsuppressoptionalinfo", "\\pdfstartthread", "\\pdfstartlink", "\\pdfsetrandomseed", "\\pdfsetmatrix", "\\pdfsavepos", "\\pdfsave", "\\pdfretval", "\\pdfrestore", "\\pdfreplacefont", "\\pdfrefximage", "\\pdfrefxform", "\\pdfrefobj", "\\pdfrecompress", "\\pdfrandomseed", "\\pdfpxdimen", "\\pdfprotrudechars", "\\pdfprimitive", "\\pdfpkresolution", "\\pdfpkmode", "\\pdfpkfixeddpi", "\\pdfpagewidth", "\\pdfpagesattr", "\\pdfpageresources", "\\pdfpageref", "\\pdfpageheight", "\\pdfpagebox", "\\pdfpageattr", "\\pdfoutput", "\\pdfoutline", "\\pdfomitcidset", "\\pdfomitcharset", "\\pdfobjcompresslevel", "\\pdfobj", "\\pdfnormaldeviate", "\\pdfnoligatures", "\\pdfnames", "\\pdfminorversion", "\\pdfmapline", "\\pdfmapfile", "\\pdfmajorversion", "\\pdfliteral", "\\pdflinkmargin", "\\pdflastypos", "\\pdflastxpos", "\\pdflastximagepages", "\\pdflastximage", "\\pdflastxform", "\\pdflastobj", "\\pdflastlink", "\\pdflastlinedepth", "\\pdflastannot", "\\pdfinsertht", "\\pdfinfoomitdate", "\\pdfinfo", "\\pdfinclusionerrorlevel", "\\pdfinclusioncopyfonts", "\\pdfincludechars", "\\pdfimageresolution", "\\pdfimagehicolor", "\\pdfimagegamma", "\\pdfimageapplygamma", "\\pdfimageaddfilename", "\\pdfignoreunknownimages", "\\pdfignoreddimen", "\\pdfhorigin", "\\pdfglyphtounicode", "\\pdfgentounicode", "\\pdfgamma", "\\pdffontsize", "\\pdffontobjnum", "\\pdffontname", "\\pdffontexpand", "\\pdffontattr", "\\pdffirstlineheight", "\\pdffeedback", "\\pdfextension", "\\pdfendthread", "\\pdfendlink", "\\pdfeachlineheight", "\\pdfeachlinedepth", "\\pdfdraftmode", "\\pdfdestmargin", "\\pdfdest", "\\pdfdecimaldigits", "\\pdfcreationdate", "\\pdfcopyfont", "\\pdfcompresslevel", "\\pdfcolorstackinit", "\\pdfcolorstack", "\\pdfcatalog", "\\pdfannot", "\\pdfadjustspacing", "\\pausing", "\\patterns", "\\parskip", "\\parshapelength", "\\parshapeindent", "\\parshapedimen", "\\parshape", "\\parindent", "\\parfillskip", "\\pardirection", "\\pardir", "\\par", "\\pagewidth", "\\pagetotal", "\\pagetopoffset", "\\pagestretch", "\\pageshrink", "\\pagerightoffset", "\\pageleftoffset", "\\pageheight", "\\pagegoal", "\\pagefilstretch", "\\pagefillstretch", "\\pagefilllstretch", "\\pagediscards", "\\pagedirection", "\\pagedir", "\\pagedepth", "\\pagebottomoffset", "\\overwithdelims", "\\overline", "\\overfullrule", "\\over", "\\outputpenalty", "\\outputmode", "\\outputbox", "\\output", "\\outer", "\\or", "\\openout", "\\openin", "\\omit", "\\numexpr", "\\number", "\\nullfont", "\\nulldelimiterspace", "\\novrule", "\\nospaces", "\\normalyear", "\\normalxtokspre", "\\normalxtoksapp", "\\normalxspaceskip", "\\normalxleaders", "\\normalxdef", "\\normalwrite", "\\normalwordboundary", "\\normalwidowpenalty", "\\normalwidowpenalties", "\\normalwd", "\\normalvtop", "\\normalvss", "\\normalvsplit", "\\normalvskip", "\\normalvsize", "\\normalvrule", "\\normalvpack", "\\normalvoffset", "\\normalvfuzz", "\\normalvfilneg", "\\normalvfill", "\\normalvfil", "\\normalvcenter", "\\normalvbox", "\\normalvbadness", "\\normalvalign", "\\normalvadjust", "\\normaluseimageresource", "\\normaluseboxresource", "\\normaluppercase", "\\normalunvcopy", "\\normalunvbox", "\\normalunskip", "\\normalunpenalty", "\\normalunless", "\\normalunkern", "\\normaluniformdeviate", "\\normalunhcopy", "\\normalunhbox", "\\normalunexpanded", "\\normalunderline", "\\normaluchyph", "\\normaluccode", "\\normaltracingstats", "\\normaltracingscantokens", "\\normaltracingrestores", "\\normaltracingparagraphs", "\\normaltracingpages", "\\normaltracingoutput", "\\normaltracingonline", "\\normaltracingnesting", "\\normaltracingmacros", "\\normaltracinglostchars", "\\normaltracingifs", "\\normaltracinggroups", "\\normaltracingfonts", "\\normaltracingcommands", "\\normaltracingassigns", "\\normaltpack", "\\normaltopskip", "\\normaltopmarks", "\\normaltopmark", "\\normaltolerance", "\\normaltokspre", "\\normaltoksdef", "\\normaltoksapp", "\\normaltoks", "\\normaltime", "\\normalthinmuskip", "\\normalthickmuskip", "\\normalthe", "\\normaltextstyle", "\\normaltextfont", "\\normaltextdirection", "\\normaltextdir", "\\normaltagcode", "\\normaltabskip", "\\normalsynctex", "\\normalsuppressprimitiveerror", "\\normalsuppressoutererror", "\\normalsuppressmathparerror", "\\normalsuppresslongerror", "\\normalsuppressifcsnameerror", "\\normalsuppressfontnotfounderror", "\\normalstring", "\\normalsplittopskip", "\\normalsplitmaxdepth", "\\normalsplitfirstmarks", "\\normalsplitfirstmark", "\\normalsplitdiscards", "\\normalsplitbotmarks", "\\normalsplitbotmark", "\\normalspecial", "\\normalspan", "\\normalspaceskip", "\\normalspacefactor", "\\normalskipdef", "\\normalskip", "\\normalskewchar", "\\normalshowtokens", "\\normalshowthe", "\\normalshowlists", "\\normalshowifs", "\\normalshowgroups", "\\normalshowboxdepth", "\\normalshowboxbreadth", "\\normalshowbox", "\\normalshow", "\\normalshipout", "\\normalshapemode", "\\normalsfcode", "\\normalsetrandomseed", "\\normalsetlanguage", "\\normalsetfontid", "\\normalsetbox", "\\normalscrollmode", "\\normalscriptstyle", "\\normalscriptspace", "\\normalscriptscriptstyle", "\\normalscriptscriptfont", "\\normalscriptfont", "\\normalscantokens", "\\normalscantextokens", "\\normalsavingvdiscards", "\\normalsavinghyphcodes", "\\normalsavepos", "\\normalsaveimageresource", "\\normalsavecatcodetable", "\\normalsaveboxresource", "\\normalrpcode", "\\normalromannumeral", "\\normalrightskip", "\\normalrightmarginkern", "\\normalrighthyphenmin", "\\normalrightghost", "\\normalright", "\\normalrelpenalty", "\\normalrelax", "\\normalreadline", "\\normalread", "\\normalrandomseed", "\\normalraise", "\\normalradical", "\\normalquitvmode", "\\normalpxdimen", "\\normalprotrusionboundary", "\\normalprotrudechars", "\\normalprotected", "\\normalprimitive", "\\normalprevgraf", "\\normalprevdepth", "\\normalpretolerance", "\\normalprerelpenalty", "\\normalprehyphenchar", "\\normalpreexhyphenchar", "\\normalpredisplaysize", "\\normalpredisplaypenalty", "\\normalpredisplaygapfactor", "\\normalpredisplaydirection", "\\normalprebinoppenalty", "\\normalposthyphenchar", "\\normalpostexhyphenchar", "\\normalpostdisplaypenalty", "\\normalpenalty", "\\normalpdfximage", "\\normalpdfxformresources", "\\normalpdfxformname", "\\normalpdfxformmargin", "\\normalpdfxformattr", "\\normalpdfxform", "\\normalpdfvorigin", "\\normalpdfvariable", "\\normalpdfuniqueresname", "\\normalpdfuniformdeviate", "\\normalpdftrailerid", "\\normalpdftrailer", "\\normalpdftracingfonts", "\\normalpdfthreadmargin", "\\normalpdfthread", "\\normalpdftexversion", "\\normalpdftexrevision", "\\normalpdftexbanner", "\\normalpdfsuppressptexinfo", "\\normalpdfsuppressoptionalinfo", "\\normalpdfstartthread", "\\normalpdfstartlink", "\\normalpdfsetrandomseed", "\\normalpdfsetmatrix", "\\normalpdfsavepos", "\\normalpdfsave", "\\normalpdfretval", "\\normalpdfrestore", "\\normalpdfreplacefont", "\\normalpdfrefximage", "\\normalpdfrefxform", "\\normalpdfrefobj", "\\normalpdfrecompress", "\\normalpdfrandomseed", "\\normalpdfpxdimen", "\\normalpdfprotrudechars", "\\normalpdfprimitive", "\\normalpdfpkresolution", "\\normalpdfpkmode", "\\normalpdfpkfixeddpi", "\\normalpdfpagewidth", "\\normalpdfpagesattr", "\\normalpdfpageresources", "\\normalpdfpageref", "\\normalpdfpageheight", "\\normalpdfpagebox", "\\normalpdfpageattr", "\\normalpdfoutput", "\\normalpdfoutline", "\\normalpdfomitcidset", "\\normalpdfomitcharset", "\\normalpdfobjcompresslevel", "\\normalpdfobj", "\\normalpdfnormaldeviate", "\\normalpdfnoligatures", "\\normalpdfnames", "\\normalpdfminorversion", "\\normalpdfmapline", "\\normalpdfmapfile", "\\normalpdfmajorversion", "\\normalpdfliteral", "\\normalpdflinkmargin", "\\normalpdflastypos", "\\normalpdflastxpos", "\\normalpdflastximagepages", "\\normalpdflastximage", "\\normalpdflastxform", "\\normalpdflastobj", "\\normalpdflastlink", "\\normalpdflastlinedepth", "\\normalpdflastannot", "\\normalpdfinsertht", "\\normalpdfinfoomitdate", "\\normalpdfinfo", "\\normalpdfinclusionerrorlevel", "\\normalpdfinclusioncopyfonts", "\\normalpdfincludechars", "\\normalpdfimageresolution", "\\normalpdfimagehicolor", "\\normalpdfimagegamma", "\\normalpdfimageapplygamma", "\\normalpdfimageaddfilename", "\\normalpdfignoreunknownimages", "\\normalpdfignoreddimen", "\\normalpdfhorigin", "\\normalpdfglyphtounicode", "\\normalpdfgentounicode", "\\normalpdfgamma", "\\normalpdffontsize", "\\normalpdffontobjnum", "\\normalpdffontname", "\\normalpdffontexpand", "\\normalpdffontattr", "\\normalpdffirstlineheight", "\\normalpdffeedback", "\\normalpdfextension", "\\normalpdfendthread", "\\normalpdfendlink", "\\normalpdfeachlineheight", "\\normalpdfeachlinedepth", "\\normalpdfdraftmode", "\\normalpdfdestmargin", "\\normalpdfdest", "\\normalpdfdecimaldigits", "\\normalpdfcreationdate", "\\normalpdfcopyfont", "\\normalpdfcompresslevel", "\\normalpdfcolorstackinit", "\\normalpdfcolorstack", "\\normalpdfcatalog", "\\normalpdfannot", "\\normalpdfadjustspacing", "\\normalpausing", "\\normalpatterns", "\\normalparskip", "\\normalparshapelength", "\\normalparshapeindent", "\\normalparshapedimen", "\\normalparshape", "\\normalparindent", "\\normalparfillskip", "\\normalpardirection", "\\normalpardir", "\\normalpar", "\\normalpagewidth", "\\normalpagetotal", "\\normalpagetopoffset", "\\normalpagestretch", "\\normalpageshrink", "\\normalpagerightoffset", "\\normalpageleftoffset", "\\normalpageheight", "\\normalpagegoal", "\\normalpagefilstretch", "\\normalpagefillstretch", "\\normalpagefilllstretch", "\\normalpagediscards", "\\normalpagedirection", "\\normalpagedir", "\\normalpagedepth", "\\normalpagebottomoffset", "\\normaloverwithdelims", "\\normaloverline", "\\normaloverfullrule", "\\normalover", "\\normaloutputpenalty", "\\normaloutputmode", "\\normaloutputbox", "\\normaloutput", "\\normalouter", "\\normalor", "\\normalopenout", "\\normalopenin", "\\normalomit", "\\normalnumexpr", "\\normalnumber", "\\normalnullfont", "\\normalnulldelimiterspace", "\\normalnovrule", "\\normalnospaces", "\\normalnormaldeviate", "\\normalnonstopmode", "\\normalnonscript", "\\normalnolimits", "\\normalnoligs", "\\normalnokerns", "\\normalnoindent", "\\normalnohrule", "\\normalnoexpand", "\\normalnoboundary", "\\normalnoalign", "\\normalnewlinechar", "\\normalmutoglue", "\\normalmuskipdef", "\\normalmuskip", "\\normalmultiply", "\\normalmuexpr", "\\normalmskip", "\\normalmoveright", "\\normalmoveleft", "\\normalmonth", "\\normalmkern", "\\normalmiddle", "\\normalmessage", "\\normalmedmuskip", "\\normalmeaning", "\\normalmaxdepth", "\\normalmaxdeadcycles", "\\normalmathsurroundskip", "\\normalmathsurroundmode", "\\normalmathsurround", "\\normalmathstyle", "\\normalmathscriptsmode", "\\normalmathscriptcharmode", "\\normalmathscriptboxmode", "\\normalmathrulethicknessmode", "\\normalmathrulesmode", "\\normalmathrulesfam", "\\normalmathrel", "\\normalmathpunct", "\\normalmathpenaltiesmode", "\\normalmathord", "\\normalmathoption", "\\normalmathopen", "\\normalmathop", "\\normalmathnolimitsmode", "\\normalmathitalicsmode", "\\normalmathinner", "\\normalmathflattenmode", "\\normalmatheqnogapstep", "\\normalmathdisplayskipmode", "\\normalmathdirection", "\\normalmathdir", "\\normalmathdelimitersmode", "\\normalmathcode", "\\normalmathclose", "\\normalmathchoice", "\\normalmathchardef", "\\normalmathchar", "\\normalmathbin", "\\normalmathaccent", "\\normalmarks", "\\normalmark", "\\normalmag", "\\normalluatexversion", "\\normalluatexrevision", "\\normalluatexbanner", "\\normalluafunctioncall", "\\normalluafunction", "\\normalluaescapestring", "\\normalluadef", "\\normalluacopyinputnodes", "\\normalluabytecodecall", "\\normalluabytecode", "\\normallpcode", "\\normallowercase", "\\normallower", "\\normallooseness", "\\normallong", "\\normallocalrightbox", "\\normallocalleftbox", "\\normallocalinterlinepenalty", "\\normallocalbrokenpenalty", "\\normallinepenalty", "\\normallinedirection", "\\normallinedir", "\\normallimits", "\\normalletterspacefont", "\\normalletcharcode", "\\normallet", "\\normalleqno", "\\normalleftskip", "\\normalleftmarginkern", "\\normallefthyphenmin", "\\normalleftghost", "\\normalleft", "\\normalleaders", "\\normallccode", "\\normallateluafunction", "\\normallatelua", "\\normallastypos", "\\normallastxpos", "\\normallastskip", "\\normallastsavedimageresourcepages", "\\normallastsavedimageresourceindex", "\\normallastsavedboxresourceindex", "\\normallastpenalty", "\\normallastnodetype", "\\normallastnamedcs", "\\normallastlinefit", "\\normallastkern", "\\normallastbox", "\\normallanguage", "\\normalkern", "\\normaljobname", "\\normalinterlinepenalty", "\\normalinterlinepenalties", "\\normalinteractionmode", "\\normalinsertpenalties", "\\normalinsertht", "\\normalinsert", "\\normalinputlineno", "\\normalinput", "\\normalinitcatcodetable", "\\normalindent", "\\normalimmediateassignment", "\\normalimmediateassigned", "\\normalimmediate", "\\normalignorespaces", "\\normalignoreligaturesinfont", "\\normalifx", "\\normalifvoid", "\\normalifvmode", "\\normalifvbox", "\\normaliftrue", "\\normalifprimitive", "\\normalifpdfprimitive", "\\normalifpdfabsnum", "\\normalifpdfabsdim", "\\normalifodd", "\\normalifnum", "\\normalifmmode", "\\normalifinner", "\\normalifincsname", "\\normalifhmode", "\\normalifhbox", "\\normaliffontchar", "\\normaliffalse", "\\normalifeof", "\\normalifdim", "\\normalifdefined", "\\normalifcsname", "\\normalifcondition", "\\normalifcat", "\\normalifcase", "\\normalifabsnum", "\\normalifabsdim", "\\normalif", "\\normalhyphenpenaltymode", "\\normalhyphenpenalty", "\\normalhyphenchar", "\\normalhyphenationmin", "\\normalhyphenationbounds", "\\normalhyphenation", "\\normalht", "\\normalhss", "\\normalhskip", "\\normalhsize", "\\normalhrule", "\\normalhpack", "\\normalholdinginserts", "\\normalhoffset", "\\normalhjcode", "\\normalhfuzz", "\\normalhfilneg", "\\normalhfill", "\\normalhfil", "\\normalhbox", "\\normalhbadness", "\\normalhangindent", "\\normalhangafter", "\\normalhalign", "\\normalgtokspre", "\\normalgtoksapp", "\\normalgluetomu", "\\normalgluestretchorder", "\\normalgluestretch", "\\normalglueshrinkorder", "\\normalglueshrink", "\\normalglueexpr", "\\normalglobaldefs", "\\normalglobal", "\\normalglet", "\\normalgleaders", "\\normalgdef", "\\normalfuturelet", "\\normalfutureexpandis", "\\normalfutureexpand", "\\normalformatname", "\\normalfontname", "\\normalfontid", "\\normalfontdimen", "\\normalfontcharwd", "\\normalfontcharic", "\\normalfontcharht", "\\normalfontchardp", "\\normalfont", "\\normalfloatingpenalty", "\\normalfixupboxesmode", "\\normalfirstvalidlanguage", "\\normalfirstmarks", "\\normalfirstmark", "\\normalfinalhyphendemerits", "\\normalfi", "\\normalfam", "\\normalexplicithyphenpenalty", "\\normalexplicitdiscretionary", "\\normalexpandglyphsinfont", "\\normalexpanded", "\\normalexpandafter", "\\normalexhyphenpenalty", "\\normalexhyphenchar", "\\normalexceptionpenalty", "\\normaleveryvbox", "\\normaleverypar", "\\normaleverymath", "\\normaleveryjob", "\\normaleveryhbox", "\\normaleveryeof", "\\normaleverydisplay", "\\normaleverycr", "\\normaletokspre", "\\normaletoksapp", "\\normalescapechar", "\\normalerrorstopmode", "\\normalerrorcontextlines", "\\normalerrmessage", "\\normalerrhelp", "\\normaleqno", "\\normalendlocalcontrol", "\\normalendlinechar", "\\normalendinput", "\\normalendgroup", "\\normalendcsname", "\\normalend", "\\normalemergencystretch", "\\normalelse", "\\normalefcode", "\\normaledef", "\\normaleTeXversion", "\\normaleTeXrevision", "\\normaleTeXminorversion", "\\normaleTeXVersion", "\\normaldvivariable", "\\normaldvifeedback", "\\normaldviextension", "\\normaldump", "\\normaldraftmode", "\\normaldp", "\\normaldoublehyphendemerits", "\\normaldivide", "\\normaldisplaywidth", "\\normaldisplaywidowpenalty", "\\normaldisplaywidowpenalties", "\\normaldisplaystyle", "\\normaldisplaylimits", "\\normaldisplayindent", "\\normaldiscretionary", "\\normaldirectlua", "\\normaldimexpr", "\\normaldimendef", "\\normaldimen", "\\normaldeviate", "\\normaldetokenize", "\\normaldelimitershortfall", "\\normaldelimiterfactor", "\\normaldelimiter", "\\normaldelcode", "\\normaldefaultskewchar", "\\normaldefaulthyphenchar", "\\normaldef", "\\normaldeadcycles", "\\normalday", "\\normalcurrentiftype", "\\normalcurrentiflevel", "\\normalcurrentifbranch", "\\normalcurrentgrouptype", "\\normalcurrentgrouplevel", "\\normalcsstring", "\\normalcsname", "\\normalcrcr", "\\normalcrampedtextstyle", "\\normalcrampedscriptstyle", "\\normalcrampedscriptscriptstyle", "\\normalcrampeddisplaystyle", "\\normalcr", "\\normalcountdef", "\\normalcount", "\\normalcopyfont", "\\normalcopy", "\\normalcompoundhyphenmode", "\\normalclubpenalty", "\\normalclubpenalties", "\\normalcloseout", "\\normalclosein", "\\normalclearmarks", "\\normalcleaders", "\\normalchardef", "\\normalchar", "\\normalcatcodetable", "\\normalcatcode", "\\normalbrokenpenalty", "\\normalbreakafterdirmode", "\\normalboxmaxdepth", "\\normalboxdirection", "\\normalboxdir", "\\normalbox", "\\normalboundary", "\\normalbotmarks", "\\normalbotmark", "\\normalbodydirection", "\\normalbodydir", "\\normalbinoppenalty", "\\normalbelowdisplayskip", "\\normalbelowdisplayshortskip", "\\normalbegingroup", "\\normalbegincsname", "\\normalbatchmode", "\\normalbadness", "\\normalautomatichyphenpenalty", "\\normalautomatichyphenmode", "\\normalautomaticdiscretionary", "\\normalattributedef", "\\normalattribute", "\\normalatopwithdelims", "\\normalatop", "\\normalaligntab", "\\normalalignmark", "\\normalaftergroup", "\\normalafterassignment", "\\normaladvance", "\\normaladjustspacing", "\\normaladjdemerits", "\\normalaccent", "\\normalabovewithdelims", "\\normalabovedisplayskip", "\\normalabovedisplayshortskip", "\\normalabove", "\\normalXeTeXversion", "\\normalUvextensible", "\\normalUunderdelimiter", "\\normalUsuperscript", "\\normalUsubscript", "\\normalUstopmath", "\\normalUstopdisplaymath", "\\normalUstartmath", "\\normalUstartdisplaymath", "\\normalUstack", "\\normalUskewedwithdelims", "\\normalUskewed", "\\normalUroot", "\\normalUright", "\\normalUradical", "\\normalUoverdelimiter", "\\normalUnosuperscript", "\\normalUnosubscript", "\\normalUmiddle", "\\normalUmathunderdelimitervgap", "\\normalUmathunderdelimiterbgap", "\\normalUmathunderbarvgap", "\\normalUmathunderbarrule", "\\normalUmathunderbarkern", "\\normalUmathsupsubbottommax", "\\normalUmathsupshiftup", "\\normalUmathsupshiftdrop", "\\normalUmathsupbottommin", "\\normalUmathsubtopmax", "\\normalUmathsubsupvgap", "\\normalUmathsubsupshiftdown", "\\normalUmathsubshiftdrop", "\\normalUmathsubshiftdown", "\\normalUmathstackvgap", "\\normalUmathstacknumup", "\\normalUmathstackdenomdown", "\\normalUmathspaceafterscript", "\\normalUmathskewedfractionvgap", "\\normalUmathskewedfractionhgap", "\\normalUmathrelrelspacing", "\\normalUmathrelpunctspacing", "\\normalUmathrelordspacing", "\\normalUmathrelopspacing", "\\normalUmathrelopenspacing", "\\normalUmathrelinnerspacing", "\\normalUmathrelclosespacing", "\\normalUmathrelbinspacing", "\\normalUmathradicalvgap", "\\normalUmathradicalrule", "\\normalUmathradicalkern", "\\normalUmathradicaldegreeraise", "\\normalUmathradicaldegreebefore", "\\normalUmathradicaldegreeafter", "\\normalUmathquad", "\\normalUmathpunctrelspacing", "\\normalUmathpunctpunctspacing", "\\normalUmathpunctordspacing", "\\normalUmathpunctopspacing", "\\normalUmathpunctopenspacing", "\\normalUmathpunctinnerspacing", "\\normalUmathpunctclosespacing", "\\normalUmathpunctbinspacing", "\\normalUmathoverdelimitervgap", "\\normalUmathoverdelimiterbgap", "\\normalUmathoverbarvgap", "\\normalUmathoverbarrule", "\\normalUmathoverbarkern", "\\normalUmathordrelspacing", "\\normalUmathordpunctspacing", "\\normalUmathordordspacing", "\\normalUmathordopspacing", "\\normalUmathordopenspacing", "\\normalUmathordinnerspacing", "\\normalUmathordclosespacing", "\\normalUmathordbinspacing", "\\normalUmathoprelspacing", "\\normalUmathoppunctspacing", "\\normalUmathopordspacing", "\\normalUmathopopspacing", "\\normalUmathopopenspacing", "\\normalUmathopinnerspacing", "\\normalUmathoperatorsize", "\\normalUmathopenrelspacing", "\\normalUmathopenpunctspacing", "\\normalUmathopenordspacing", "\\normalUmathopenopspacing", "\\normalUmathopenopenspacing", "\\normalUmathopeninnerspacing", "\\normalUmathopenclosespacing", "\\normalUmathopenbinspacing", "\\normalUmathopclosespacing", "\\normalUmathopbinspacing", "\\normalUmathnolimitsupfactor", "\\normalUmathnolimitsubfactor", "\\normalUmathlimitbelowvgap", "\\normalUmathlimitbelowkern", "\\normalUmathlimitbelowbgap", "\\normalUmathlimitabovevgap", "\\normalUmathlimitabovekern", "\\normalUmathlimitabovebgap", "\\normalUmathinnerrelspacing", "\\normalUmathinnerpunctspacing", "\\normalUmathinnerordspacing", "\\normalUmathinneropspacing", "\\normalUmathinneropenspacing", "\\normalUmathinnerinnerspacing", "\\normalUmathinnerclosespacing", "\\normalUmathinnerbinspacing", "\\normalUmathfractionrule", "\\normalUmathfractionnumvgap", "\\normalUmathfractionnumup", "\\normalUmathfractiondenomvgap", "\\normalUmathfractiondenomdown", "\\normalUmathfractiondelsize", "\\normalUmathconnectoroverlapmin", "\\normalUmathcodenum", "\\normalUmathcode", "\\normalUmathcloserelspacing", "\\normalUmathclosepunctspacing", "\\normalUmathcloseordspacing", "\\normalUmathcloseopspacing", "\\normalUmathcloseopenspacing", "\\normalUmathcloseinnerspacing", "\\normalUmathcloseclosespacing", "\\normalUmathclosebinspacing", "\\normalUmathcharslot", "\\normalUmathcharnumdef", "\\normalUmathcharnum", "\\normalUmathcharfam", "\\normalUmathchardef", "\\normalUmathcharclass", "\\normalUmathchar", "\\normalUmathbinrelspacing", "\\normalUmathbinpunctspacing", "\\normalUmathbinordspacing", "\\normalUmathbinopspacing", "\\normalUmathbinopenspacing", "\\normalUmathbininnerspacing", "\\normalUmathbinclosespacing", "\\normalUmathbinbinspacing", "\\normalUmathaxis", "\\normalUmathaccent", "\\normalUleft", "\\normalUhextensible", "\\normalUdelimiterunder", "\\normalUdelimiterover", "\\normalUdelimiter", "\\normalUdelcodenum", "\\normalUdelcode", "\\normalUchar", "\\normalOmegaversion", "\\normalOmegarevision", "\\normalOmegaminorversion", "\\normalAlephversion", "\\normalAlephrevision", "\\normalAlephminorversion", "\\normal", "\\nonstopmode", "\\nonscript", "\\nolimits", "\\noligs", "\\nokerns", "\\noindent", "\\nohrule", "\\noexpand", "\\noboundary", "\\noalign", "\\newlinechar", "\\mutoglue", "\\muskipdef", "\\muskip", "\\multiply", "\\muexpr", "\\mskip", "\\moveright", "\\moveleft", "\\month", "\\mkern", "\\middle", "\\message", "\\medmuskip", "\\meaning", "\\maxdepth", "\\maxdeadcycles", "\\mathsurroundskip", "\\mathsurroundmode", "\\mathsurround", "\\mathstyle", "\\mathscriptsmode", "\\mathscriptcharmode", "\\mathscriptboxmode", "\\mathrulethicknessmode", "\\mathrulesmode", "\\mathrulesfam", "\\mathrel", "\\mathpunct", "\\mathpenaltiesmode", "\\mathord", "\\mathoption", "\\mathopen", "\\mathop", "\\mathnolimitsmode", "\\mathitalicsmode", "\\mathinner", "\\mathflattenmode", "\\matheqnogapstep", "\\mathdisplayskipmode", "\\mathdirection", "\\mathdir", "\\mathdelimitersmode", "\\mathcode", "\\mathclose", "\\mathchoice", "\\mathchardef", "\\mathchar", "\\mathbin", "\\mathaccent", "\\marks", "\\mark", "\\mag", "\\luatexversion", "\\luatexrevision", "\\luatexbanner", "\\luafunctioncall", "\\luafunction", "\\luaescapestring", "\\luadef", "\\luacopyinputnodes", "\\luabytecodecall", "\\luabytecode", "\\lpcode", "\\lowercase", "\\lower", "\\looseness", "\\long", "\\localrightbox", "\\localleftbox", "\\localinterlinepenalty", "\\localbrokenpenalty", "\\lineskiplimit", "\\lineskip", "\\linepenalty", "\\linedirection", "\\linedir", "\\limits", "\\letterspacefont", "\\letcharcode", "\\let", "\\leqno", "\\leftskip", "\\leftmarginkern", "\\lefthyphenmin", "\\leftghost", "\\left", "\\leaders", "\\lccode", "\\lateluafunction", "\\latelua", "\\lastypos", "\\lastxpos", "\\lastskip", "\\lastsavedimageresourcepages", "\\lastsavedimageresourceindex", "\\lastsavedboxresourceindex", "\\lastpenalty", "\\lastnodetype", "\\lastnamedcs", "\\lastlinefit", "\\lastkern", "\\lastbox", "\\language", "\\kern", "\\jobname", "\\interlinepenalty", "\\interlinepenalties", "\\interactionmode", "\\insertpenalties", "\\insertht", "\\insert", "\\inputlineno", "\\input", "\\initcatcodetable", "\\indent", "\\immediateassignment", "\\immediateassigned", "\\immediate", "\\ignorespaces", "\\ignoreligaturesinfont", "\\ifx", "\\ifvoid", "\\ifvmode", "\\ifvbox", "\\iftrue", "\\ifprimitive", "\\ifpdfprimitive", "\\ifpdfabsnum", "\\ifpdfabsdim", "\\ifodd", "\\ifnum", "\\ifmmode", "\\ifinner", "\\ifincsname", "\\ifhmode", "\\ifhbox", "\\iffontchar", "\\iffalse", "\\ifeof", "\\ifdim", "\\ifdefined", "\\ifcsname", "\\ifcondition", "\\ifcat", "\\ifcase", "\\ifabsnum", "\\ifabsdim", "\\if", "\\hyphenpenaltymode", "\\hyphenpenalty", "\\hyphenchar", "\\hyphenationmin", "\\hyphenationbounds", "\\hyphenation", "\\ht", "\\hss", "\\hskip", "\\hsize", "\\hrule", "\\hpack", "\\holdinginserts", "\\hoffset", "\\hjcode", "\\hfuzz", "\\hfilneg", "\\hfill", "\\hfil", "\\hbox", "\\hbadness", "\\hangindent", "\\hangafter", "\\halign", "\\gtokspre", "\\gtoksapp", "\\gluetomu", "\\gluestretchorder", "\\gluestretch", "\\glueshrinkorder", "\\glueshrink", "\\glueexpr", "\\globaldefs", "\\global", "\\gleaders", "\\gdef", "\\futurelet", "\\futureexpandis", "\\futureexpand", "\\formatname", "\\fontname", "\\fontid", "\\fontdimen", "\\fontcharwd", "\\fontcharic", "\\fontcharht", "\\fontchardp", "\\font", "\\floatingpenalty", "\\fixupboxesmode", "\\firstvalidlanguage", "\\firstmarks", "\\firstmark", "\\finalhyphendemerits", "\\fi", "\\fam", "\\explicithyphenpenalty", "\\explicitdiscretionary", "\\expandglyphsinfont", "\\expandafter", "\\exhyphenpenalty", "\\exhyphenchar", "\\exceptionpenalty", "\\everyvbox", "\\everypar", "\\everymath", "\\everyjob", "\\everyhbox", "\\everyeof", "\\everydisplay", "\\everycr", "\\etokspre", "\\etoksapp", "\\escapechar", "\\errorstopmode", "\\errorcontextlines", "\\errmessage", "\\errhelp", "\\eqno", "\\endlocalcontrol", "\\endlinechar", "\\endinput", "\\endgroup", "\\endcsname", "\\end", "\\emergencystretch", "\\else", "\\efcode", "\\edef", "\\eTeXversion", "\\eTeXrevision", "\\eTeXminorversion", "\\eTeXVersion", "\\dvivariable", "\\dvifeedback", "\\dviextension", "\\dump", "\\draftmode", "\\dp", "\\doublehyphendemerits", "\\divide", "\\displaywidth", "\\displaywidowpenalty", "\\displaywidowpenalties", "\\displaystyle", "\\displaylimits", "\\displayindent", "\\discretionary", "\\directlua", "\\dimexpr", "\\dimendef", "\\dimen", "\\detokenize", "\\delimitershortfall", "\\delimiterfactor", "\\delimiter", "\\delcode", "\\defaultskewchar", "\\defaulthyphenchar", "\\def", "\\deadcycles", "\\day", "\\currentiftype", "\\currentiflevel", "\\currentifbranch", "\\currentgrouptype", "\\currentgrouplevel", "\\csstring", "\\csname", "\\crcr", "\\crampedtextstyle", "\\crampedscriptstyle", "\\crampedscriptscriptstyle", "\\crampeddisplaystyle", "\\cr", "\\countdef", "\\count", "\\copyfont", "\\copy", "\\compoundhyphenmode", "\\clubpenalty", "\\clubpenalties", "\\closeout", "\\closein", "\\clearmarks", "\\cleaders", "\\chardef", "\\char", "\\catcodetable", "\\catcode", "\\brokenpenalty", "\\breakafterdirmode", "\\boxmaxdepth", "\\boxdirection", "\\boxdir", "\\box", "\\boundary", "\\botmarks", "\\botmark", "\\bodydirection", "\\bodydir", "\\binoppenalty", "\\belowdisplayskip", "\\belowdisplayshortskip", "\\begingroup", "\\begincsname", "\\batchmode", "\\baselineskip", "\\badness", "\\automatichyphenpenalty", "\\automatichyphenmode", "\\automaticdiscretionary", "\\attributedef", "\\attribute", "\\atopwithdelims", "\\atop", "\\aligntab", "\\alignmark", "\\aftergroup", "\\afterassignment", "\\advance", "\\adjustspacing", "\\adjdemerits", "\\accent", "\\abovewithdelims", "\\abovedisplayskip", "\\abovedisplayshortskip", "\\above", "\\XeTeXversion", "\\Uvextensible", "\\Uunderdelimiter", "\\Usuperscript", "\\Usubscript", "\\Ustopmath", "\\Ustopdisplaymath", "\\Ustartmath", "\\Ustartdisplaymath", "\\Ustack", "\\Uskewedwithdelims", "\\Uskewed", "\\Uroot", "\\Uright", "\\Uradical", "\\Uoverdelimiter", "\\Unosuperscript", "\\Unosubscript", "\\Umiddle", "\\Umathunderdelimitervgap", "\\Umathunderdelimiterbgap", "\\Umathunderbarvgap", "\\Umathunderbarrule", "\\Umathunderbarkern", "\\Umathsupsubbottommax", "\\Umathsupshiftup", "\\Umathsupshiftdrop", "\\Umathsupbottommin", "\\Umathsubtopmax", "\\Umathsubsupvgap", "\\Umathsubsupshiftdown", "\\Umathsubshiftdrop", "\\Umathsubshiftdown", "\\Umathstackvgap", "\\Umathstacknumup", "\\Umathstackdenomdown", "\\Umathspaceafterscript", "\\Umathskewedfractionvgap", "\\Umathskewedfractionhgap", "\\Umathrelrelspacing", "\\Umathrelpunctspacing", "\\Umathrelordspacing", "\\Umathrelopspacing", "\\Umathrelopenspacing", "\\Umathrelinnerspacing", "\\Umathrelclosespacing", "\\Umathrelbinspacing", "\\Umathradicalvgap", "\\Umathradicalrule", "\\Umathradicalkern", "\\Umathradicaldegreeraise", "\\Umathradicaldegreebefore", "\\Umathradicaldegreeafter", "\\Umathquad", "\\Umathpunctrelspacing", "\\Umathpunctpunctspacing", "\\Umathpunctordspacing", "\\Umathpunctopspacing", "\\Umathpunctopenspacing", "\\Umathpunctinnerspacing", "\\Umathpunctclosespacing", "\\Umathpunctbinspacing", "\\Umathoverdelimitervgap", "\\Umathoverdelimiterbgap", "\\Umathoverbarvgap", "\\Umathoverbarrule", "\\Umathoverbarkern", "\\Umathordrelspacing", "\\Umathordpunctspacing", "\\Umathordordspacing", "\\Umathordopspacing", "\\Umathordopenspacing", "\\Umathordinnerspacing", "\\Umathordclosespacing", "\\Umathordbinspacing", "\\Umathoprelspacing", "\\Umathoppunctspacing", "\\Umathopordspacing", "\\Umathopopspacing", "\\Umathopopenspacing", "\\Umathopinnerspacing", "\\Umathoperatorsize", "\\Umathopenrelspacing", "\\Umathopenpunctspacing", "\\Umathopenordspacing", "\\Umathopenopspacing", "\\Umathopenopenspacing", "\\Umathopeninnerspacing", "\\Umathopenclosespacing", "\\Umathopenbinspacing", "\\Umathopclosespacing", "\\Umathopbinspacing", "\\Umathnolimitsupfactor", "\\Umathnolimitsubfactor", "\\Umathlimitbelowvgap", "\\Umathlimitbelowkern", "\\Umathlimitbelowbgap", "\\Umathlimitabovevgap", "\\Umathlimitabovekern", "\\Umathlimitabovebgap", "\\Umathinnerrelspacing", "\\Umathinnerpunctspacing", "\\Umathinnerordspacing", "\\Umathinneropspacing", "\\Umathinneropenspacing", "\\Umathinnerinnerspacing", "\\Umathinnerclosespacing", "\\Umathinnerbinspacing", "\\Umathfractionrule", "\\Umathfractionnumvgap", "\\Umathfractionnumup", "\\Umathfractiondenomvgap", "\\Umathfractiondenomdown", "\\Umathfractiondelsize", "\\Umathconnectoroverlapmin", "\\Umathcodenum", "\\Umathcode", "\\Umathcloserelspacing", "\\Umathclosepunctspacing", "\\Umathcloseordspacing", "\\Umathcloseopspacing", "\\Umathcloseopenspacing", "\\Umathcloseinnerspacing", "\\Umathcloseclosespacing", "\\Umathclosebinspacing", "\\Umathcharslot", "\\Umathcharnumdef", "\\Umathcharnum", "\\Umathcharfam", "\\Umathchardef", "\\Umathcharclass", "\\Umathchar", "\\Umathbinrelspacing", "\\Umathbinpunctspacing", "\\Umathbinordspacing", "\\Umathbinopspacing", "\\Umathbinopenspacing", "\\Umathbininnerspacing", "\\Umathbinclosespacing", "\\Umathbinbinspacing", "\\Umathaxis", "\\Umathaccent", "\\Uleft", "\\Uhextensible", "\\Udelimiterunder", "\\Udelimiterover", "\\Udelimiter", "\\Udelcodenum", "\\Udelcode", "\\Uchar", "\\Omegaversion", "\\Omegarevision", "\\Omegaminorversion", "\\Alephversion", "\\Alephrevision", "\\Alephminorversion" } },
                },
                FoldingPairs = new()
                {
                    new SyntaxFolding() { RegexStart = /*language=regex*/ @"\\(start).+?\b", RegexEnd = /*language=regex*/ @"\\(stop).+?\b" },
                },
                CommandTriggerCharacters = new[] { '\\' },
                OptionsTriggerCharacters = new[] { '[' },
            },

            new("Lua")
            {
                FoldingPairs = new()
                {
                    new() { RegexStart = /*language=regex*/ @"\bfunction\b", RegexEnd = /*language=regex*/ @"\bend\b" },
                    new() { RegexStart = /*language=regex*/ @"\bfor\b", RegexEnd = /*language=regex*/ @"\bend\b" },
                    new() { RegexStart = /*language=regex*/ @"\bwhile\b", RegexEnd = /*language=regex*/ @"\bend\b" },
                    new() { RegexStart = /*language=regex*/ @"\bif\b", RegexEnd = /*language=regex*/ @"\bend\b" },
                },
                RegexTokens = new()
                {
                    { Token.Math, /*language=regex*/ @"\b(math)\.(pi|a?tan|atan2|tanh|a?cos|cosh|a?sin|sinh|max|pi|min|ceil|floor|(fr|le)?exp|pow|fmod|modf|random(seed)?|sqrt|log(10)?|deg|rad|abs)\b" },
                    { Token.Array, /*language=regex*/ @"\b((table)\.(insert|concat|sort|remove|maxn)|(string)\.(insert|sub|rep|reverse|format|len|find|byte|char|dump|lower|upper|g?match|g?sub|format|formatters))\b" },
                    { Token.Symbol, /*language=regex*/ @"[:=<>,.!?&%+\|\-*\/\^~;]" },
                    { Token.Bracket, /*language=regex*/ @"[\[\]\(\)\{\}]" },
                    { Token.Number, /*language=regex*/ @"0[xX][0-9a-fA-F]*|-?\d*\.\d+([eE][\-+]?\d+)?|-?\d+?" },
                    { Token.String, /*language=regex*/ "\\\".*?\\\"|'.*?'" },
                    { Token.Comment, /*language=regex*/ "\\\"[^\\\"]*\\\" | --.*?\\\n" },
                },
                WordTokens = new()
                {
                    { Token.Keyword, new string[] { "local", "true", "false", "in", "else", "not", "or", "and", "then", "nil", "end", "do", "repeat", "goto", "until", "return", "break" } },
                    { Token.Environment, new string[] { "function", "end", "if", "elseif", "else", "while", "for", } },
                    { Token.Function, new string[] { "#", "assert", "collectgarbage", "dofile", "_G", "getfenv", "ipairs", "load", "loadstring", "pairs", "pcall", "print", "rawequal", "rawget", "rawset", "select", "setfenv", "_VERSION", "xpcall", "module", "require", "tostring", "tonumber", "type", "rawset", "setmetatable", "getmetatable", "error", "unpack", "next", } }
                },
            },
            new("Log")
            {
                FoldingPairs = new()
                {

                },
                RegexTokens = new()
                {
                    { Token.Keyword, /*language=regex*/ @"^[\w ]*?(?=>)" },
                    { Token.Command, /*language=regex*/ @"\\.+?\b" },
                    { Token.Symbol, /*language=regex*/ @"[:=<>,.!?&%+\|\-*\/\^~;]" },
                    { Token.Bracket, /*language=regex*/ @"[\[\]\(\)\{\}]" },
                    { Token.Number, /*language=regex*/ @"0[xX][0-9a-fA-F]*|-?\d*\.\d+([eE][\-+]?\d+)?|-?\d+?" },
                },
                WordTokens = new()
                {
                    
                },
            }
        };
    }
}