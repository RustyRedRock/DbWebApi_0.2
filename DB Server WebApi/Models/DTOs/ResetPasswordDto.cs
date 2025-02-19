using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;

namespace DB_Server_WebApi.Models.DTOs
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "L'adresse e-mail est requise.")]
        [EmailAddress(ErrorMessage = "L'adresse e-mail n'est pas valide.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le jeton est requis.")]
        public string Token { get; set; }

        [Required(ErrorMessage = "Le nouveau mot de passe est requis.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le nouveau mot de passe doit contenir au moins 6 caractères.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Le nouveau mot de passe et le mot de passe de confirmation ne correspondent pas.")]
        public string ConfirmPassword { get; set; } //Champ pour confirmer le nouveau mot de passe
    }
}
/*
 L'erreur HTTP 405 (Method Not Allowed) indique que la méthode HTTP utilisée (GET, POST, PUT, DELETE, etc.) n'est pas autorisée pour la ressource demandée. Dans votre cas, vous essayez d'accéder à /api/auth/resetpassword avec une requête GET (probablement en cliquant sur le lien dans l'e-mail), alors que votre endpoint resetpassword dans AuthController est configuré pour accepter uniquement les requêtes POST ([HttpPost("resetpassword")]).

Pourquoi une Requête GET ?

Lorsque vous cliquez sur un lien dans un e-mail, votre navigateur envoie une requête GET à l'URL spécifiée dans le lien. C'est le comportement standard des liens hypertextes.

Solutions

Vous avez plusieurs options pour résoudre ce problème, chacune avec ses avantages et ses inconvénients :

1. (Recommandé pour WebGL) Utiliser une Page Web Intermédiaire pour la Réinitialisation (Redirection vers le Jeu)

Principe:

L'URL dans l'e-mail pointe vers une page web simple (HTML, JavaScript) hébergée sur votre serveur web (pas votre API).
Cette page web extrait les paramètres email et token de l'URL.
La page web affiche un formulaire de réinitialisation de mot de passe (avec les champs "Nouveau mot de passe" et "Confirmer le nouveau mot de passe"). Le champ e-mail peut être pré-rempli et caché.
Lorsque l'utilisateur soumet le formulaire, la page web envoie une requête POST à votre API (/api/auth/resetpassword) avec l'email, le token, et le nouveau mot de passe.
Après une réinitialisation réussie, la page web redirige l'utilisateur vers votre jeu (soit automatiquement, soit via un bouton "Retour au jeu").
Avantages:

Sécurité: Le token n'est jamais directement exposé dans le code de votre jeu (il est traité côté serveur et dans la page web intermédiaire).
Expérience utilisateur: Vous avez un contrôle total sur l'apparence et le comportement de la page de réinitialisation.
Compatibilité: Fonctionne avec tous les types de clients (WebGL, applications autonomes, etc.).
Conformité: Suit le processus standard.
Inconvénients:

Plus complexe à mettre en œuvre: Vous devez créer une page web supplémentaire.
Hébergement : Vous devez héberger cette page, ce qui peut être fait sur le même serveur que votre API, ou sur un serveur web statique.
Exemple (Page Web Intermédiaire - resetpassword.html):

 ```html
 <!DOCTYPE html>
 <html>
 <head>
     <title>Réinitialisation du mot de passe</title>
 </head>
 <body>
     <h1>Réinitialisation du mot de passe</h1>

     <form id="resetPasswordForm">
         <input type="hidden" id="email" name="email" value="">
         <input type="hidden" id="token" name="token" value="">

         <label for="newPassword">Nouveau mot de passe:</label><br>
         <input type="password" id="newPassword" name="newPassword" required><br><br>

         <label for="confirmPassword">Confirmer le nouveau mot de passe:</label><br>
         <input type="password" id="confirmPassword" name="confirmPassword" required><br><br>

         <button type="submit">Réinitialiser le mot de passe</button>
     </form>

     <p id="message"></p>

     <script>
         // Extraire les paramètres de l'URL
         const urlParams = new URLSearchParams(window.location.search);
         const email = urlParams.get('email');
         const token = urlParams.get('token');

         // Pré-remplir les champs cachés
         document.getElementById('email').value = email;
         document.getElementById('token').value = token;

         // Gérer la soumission du formulaire
         document.getElementById('resetPasswordForm').addEventListener('submit', async (event) => {
             event.preventDefault(); // Empêcher le comportement par défaut du formulaire

             const newPassword = document.getElementById('newPassword').value;
             const confirmPassword = document.getElementById('confirmPassword').value;

             if (newPassword !== confirmPassword) {
                 document.getElementById('message').textContent = "Les mots de passe ne correspondent pas.";
                 return;
             }

             // Envoyer la requête POST à l'API
             const response = await fetch('/api/auth/resetpassword', { // Assurez vous d'utiliser l'url de base de votre API
                 method: 'POST',
                 headers: {
                     'Content-Type': 'application/json'
                 },
                 body: JSON.stringify({
                     email: email,
                     token: token,
                     newPassword: newPassword,
                     confirmPassword : confirmPassword //On envoie aussi la confirmation
                 })
             });

             if (response.ok) {
                 document.getElementById('message').textContent = "Votre mot de passe a été réinitialisé avec succès.";
                  // Rediriger vers le jeu après un délai (par exemple, 3 secondes)
                  setTimeout(() => { window.location.href = "https://yourgame.com"; }, 3000); // Redirigez vers votre jeu !
             } else {
                  const errorData = await response.json(); //Récupère les informations de l'erreur
                 document.getElementById('message').textContent = "Erreur: " + (errorData.message || "Une erreur s'est produite."); //Permet d'afficher les messages plus précis.
             }
         });
     </script>
 </body>
 </html>
 ```
Configuration du Serveur Web: Configurez votre serveur web (Apache, Nginx, etc.) pour servir ce fichier HTML lorsque l'URL /resetpassword est demandée. Par exemple, avec Apache et un fichier .htaccess :
RewriteEngine On
RewriteRule ^resetpassword$ resetpassword.html [L]
Changement de l'URL de base : Dans votre appsettings.json et appsettings.Development.json, changez le "ResetPasswordUrlBase" pour pointer vers l'endroit ou est héberger la page web resetpassword.html.
JSON

 "ResetPasswordUrlBase": "https://yourgame.com/resetpassword" //Pour une page web externe au jeu
(Déconseillé, Sauf Cas Très Spécifiques) Modifier le Contrôleur pour Accepter GET (Très Mauvaise Pratique):

Principe:  Vous pourriez modifier votre action ResetPassword dans AuthController pour qu'elle accepte les requêtes GET en plus des requêtes POST.  Vous extrairiez les paramètres email et token de la chaîne de requête (comme vous le faites déjà dans ConfirmEmail), puis vous afficheriez un formulaire de réinitialisation de mot de passe (en utilisant une vue Razor, par exemple).  Lorsque l'utilisateur soumet le formulaire, vous enverriez une requête POST à la même action ResetPassword.

Inconvénients:

Sécurité: Exposer le token de réinitialisation dans l'URL (dans l'historique du navigateur, dans les logs du serveur, etc.) est une très mauvaise pratique.
Complexité: Cela complique inutilement votre contrôleur.
Non standard : Ce n'est pas la façon standard de gérer la réinitialisation des mot de passe.
Si vous devez utiliser cette approche (ce que je déconseille fortement):

C#

[HttpGet("resetpassword")] // Ajoutez cette ligne !
[HttpPost("resetpassword")]
public async Task<IActionResult> ResetPassword([FromQuery] string email, [FromQuery] string token, [FromBody] ResetPasswordDto resetPasswordDto)
{
    if (HttpMethods.IsGet(Request.Method))
    {
        // Affiche le formulaire de réinitialisation (vous devriez utiliser une vue Razor)
        // ... (créer un ViewModel, passer email et token à la vue, etc.) ...
        // return View(viewModel); // Si vous utilisez des vues Razor
        return Content("Affichez le formulaire ici. Email: " + email + ", Token: " + token, "text/html"); //Pour un test rapide.
    }
    else if (HttpMethods.IsPost(Request.Method))
    {
        // Traite la soumission du formulaire
        if (!ModelState.IsValid) { ... }
        // ... (le reste de votre code de réinitialisation de mot de passe) ...
    }
    else
{
    return BadRequest(); //Méthode non supporté
}
}
[HttpGet("resetpassword")]: Ajoute la prise en charge des requêtes GET.
[FromQuery]: Indique que email et token doivent être lus à partir de la chaîne de requête (l'URL).
[FromBody]: Indique que resetPasswordDto doit être lu à partir du corps de la requête (pour la requête POST).
HttpMethods.IsGet et HttpMethods.IsPost : Permet de distinguer la requête pour afficher le formulaire, et la requête pour réinitialiser le mot de passe.
Vue razor : Normalement, on utilise une vue Razor pour afficher le formulaire.
Recommandation Finale:

Utilisez la première solution (page web intermédiaire). C'est la méthode la plus propre, la plus sûre, et la plus standard. Elle est également compatible avec tous les types de clients (WebGL, applications autonomes, etc.). Évitez la deuxième solution (modifier le contrôleur pour accepter GET) sauf si vous avez une très bonne raison de le faire, et que vous comprenez parfaitement les implications en termes de sécurité.
 */