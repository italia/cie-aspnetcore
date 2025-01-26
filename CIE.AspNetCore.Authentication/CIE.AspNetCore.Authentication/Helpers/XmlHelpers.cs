using CIE.AspNetCore.Authentication.Resources;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace CIE.AspNetCore.Authentication.Helpers
{
    internal static class XmlHelpers
    {
        /// <summary>
        /// Signs the XML document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="referenceUri">The reference URI.</param>
        /// <param name="signatureMethod">The signature method.</param>
        /// <param name="digestMethod">The digest method.</param>
        /// <returns></returns>
        /// <exception cref="FieldAccessException"></exception>
        internal static XmlElement SignXMLDoc(XmlDocument doc,
            X509Certificate2 certificate,
            string referenceUri,
            string signatureMethod,
            string digestMethod)
        {
            BusinessValidation.ValidationNotNull(doc, ErrorLocalization.XmlDocNull);
            BusinessValidation.ValidationNotNull(certificate, ErrorLocalization.CertificateNull);
            BusinessValidation.ValidationNotNullNotWhitespace(referenceUri, ErrorLocalization.ReferenceUriNullOrWhitespace);

            AsymmetricAlgorithm privateKey;

            try
            {
                privateKey = certificate.GetRSAPrivateKey();
            }
            catch (Exception ex)
            {
                throw new FieldAccessException(ErrorLocalization.PrivateKeyNotFound, ex);
            }

            SignedXml signedXml = new SignedXml(doc)
            {
                SigningKey = privateKey
            };

            signedXml.SignedInfo.SignatureMethod = signatureMethod;
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

            Reference reference = new Reference
            {
                DigestMethod = digestMethod,
                Uri = "#" + referenceUri
            };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigExcC14NTransform());
            signedXml.AddReference(reference);

            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(certificate));
            signedXml.KeyInfo = keyInfo;
            signedXml.ComputeSignature();

            return signedXml.GetXml();
        }

        /// <summary>
        /// Verifies the signature.
        /// </summary>
        /// <param name="signedDocument">The signed document.</param>
        /// <param name="xmlMetadata">The XML metadata.</param>
        /// <returns></returns>
        internal static bool VerifySignature(XmlDocument signedDocument, Saml.IdP.EntityDescriptorType xmlMetadata = null)
        {
            BusinessValidation.Argument(signedDocument, string.Format(ErrorLocalization.ParameterCantNullOrEmpty, nameof(signedDocument)));

            try
            {
                if (xmlMetadata is not null)
                {
                    var validated = false;
                    var idpSSODescriptor = xmlMetadata.Items.FirstOrDefault(i => i is Saml.IdP.IDPSSODescriptorType) as Saml.IdP.IDPSSODescriptorType;
                    if (idpSSODescriptor is not null)
                    {
                        foreach (var keyDescriptor in idpSSODescriptor.KeyDescriptor.Where(k => k.use == Saml.IdP.KeyTypes.signing))
                        {
                            var keyData = keyDescriptor.KeyInfo.Items.FirstOrDefault(i => i is Saml.IdP.X509DataType) as Saml.IdP.X509DataType;
                            if (keyData is not null)
                            {
                                var x509Cert = keyData.Items.FirstOrDefault(i => i is byte[]) as byte[];
                                if (x509Cert is not null)
                                {
                                    validated |= VerifyAllSignatures(signedDocument, new X509Certificate2(x509Cert));
                                }
                            }
                        }
                    }
                    return validated;
                }
                else
                {
                    return VerifyAllSignatures(signedDocument);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool VerifyAllSignatures(XmlDocument signedDocument, X509Certificate2? publicMetadataCert = null)
        {
            bool internalResult = true;

            XmlNodeList signatureNodes = signedDocument.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);

            if (signatureNodes.Count == 0)
            {
                return false;
            }

            foreach (var signatureNode in signatureNodes)
            {
                SignedXml signedXml = new(signedDocument);
                signedXml.LoadXml((XmlElement)signatureNode);
                internalResult &= publicMetadataCert is null
                    ? signedXml.CheckSignature()
                    : signedXml.CheckSignature(publicMetadataCert, true);
            }
            return internalResult;
        }
    }
}
