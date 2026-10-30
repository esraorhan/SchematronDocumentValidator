<?xml version="1.0" encoding="utf-8"?>
<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" queryBinding="xslt2">
  <sch:title>Özel belge doğrulama kuralları</sch:title>
  <sch:ns prefix="inv" uri="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" />
  <sch:ns prefix="cbc" uri="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2" />
  <sch:pattern id="pattern-d82d1e134912431f8f7b2906e9244e5c">
    <sch:rule context="inv:Invoice">
      <sch:assert id="assert-d82d1e134912431f8f7b2906e9244e5c" test="starts-with(normalize-space(cbc:ID), 'GIB')">Fatura numarası GIB ile başlamalıdır.</sch:assert>
    </sch:rule>
  </sch:pattern>
</sch:schema>