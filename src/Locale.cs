using System;
using System.Collections.Generic;
using System.Globalization;

namespace HedgeModManager.Improved
{
    public static class L
    {
        public static readonly string[] Codes={"en","es","pt-BR","it","fr"};
        public static readonly string[] Names={"English","Español","Português (Brasil)","Italiano","Français"};
        public static readonly Dictionary<string,string[]> Catalog=new Dictionary<string,string[]>();
        static int language;
        public static string Current {get{return Codes[language];}}
        public static void Select(string code){language=Array.IndexOf(Codes,code);if(language<0)language=0;}
        public static string T(string key){string[] values;return Catalog.TryGetValue(key,out values)?values[language]:key;}
        public static string F(string key,params object[] args){return string.Format(CultureInfo.InvariantCulture,T(key),args);}
        static readonly Dictionary<string,string> ServiceKeys=new Dictionary<string,string>{
            {"No update server","Sem servidor de atualizações"},{"Check failed: ","Falha na verificação: "},{"No VersionString","Versão remota não informada"},
            {"Update available","Atualização disponível"},{"Up to date","Atualizado"},{"Updated","Atualizado"},{"No update to apply.","Nenhuma atualização para aplicar."},
            {"Update applied.","Atualização aplicada."},{"Failed to fetch mod_files.txt: ","Falha ao baixar mod_files.txt: "},{"Invalid install request.","Solicitação de instalação inválida."},
            {"Download failed: ","Falha no download: "},{"Unsupported archive (extract failed): ","Falha ao extrair o arquivo: "},{"No mod.ini found in archive.","mod.ini não encontrado no arquivo."},
            {"Installed to ","Instalado em "},{"Link is empty.","O link está vazio."},{"Invalid install URL.","Link de instalação inválido."}
        };
        public static string Service(string text){
            if(text==null)return "";string best=null,key=null;
            foreach(var pair in ServiceKeys)if(text.StartsWith(pair.Key,StringComparison.Ordinal)&&(best==null||pair.Key.Length>best.Length)){best=pair.Key;key=pair.Value;}
            return best==null?text:T(key)+text.Substring(best.Length);
        }
        static void Add(string pt,string en,string es,string it,string fr){Catalog.Add(pt,new[]{en,es,pt,it,fr});}
        static L(){
            Add("Meus mods","My mods","Mis mods","Le mie mod","Mes mods");
            Add("Instalar","Install","Instalar","Installa","Installer");
            Add("Atualizações","Updates","Actualizaciones","Aggiornamenti","Mises à jour");
            Add("Idioma","Language","Idioma","Lingua","Langue");
            Add("Aplicar","Apply","Aplicar","Applica","Appliquer");
            Add("Idioma alterado.","Language changed.","Idioma cambiado.","Lingua modificata.","Langue modifiée.");
            Add("Escolha a pasta do jogo ou conecte a unidade E:.","Choose the game folder or connect drive E:.","Elige la carpeta del juego o conecta la unidad E:.","Scegli la cartella del gioco o collega l'unità E:.","Choisissez le dossier du jeu ou connectez le lecteur E:.");
            Add("Nenhuma pasta selecionada","No folder selected","Ninguna carpeta seleccionada","Nessuna cartella selezionata","Aucun dossier sélectionné");
            Add("Buscar mods…","Search mods…","Buscar mods…","Cerca mod…","Rechercher des mods…");
            Add("Salvar alterações","Save changes","Guardar cambios","Salva modifiche","Enregistrer");
            Add("X Configurar","X Configure","X Configurar","X Configura","X Configurer");
            Add("Y Ativar/desativar","Y Enable/disable","Y Activar/desactivar","Y Attiva/disattiva","Y Activer/désactiver");
            Add("Y Ativar","Y Enable","Y Activar","Y Attiva","Y Activer");
            Add("Y Desativar","Y Disable","Y Desactivar","Y Disattiva","Y Désactiver");
            Add("LT ↑ Prioridade","LT ↑ Priority","LT ↑ Prioridad","LT ↑ Priorità","LT ↑ Priorité");
            Add("RT ↓ Prioridade","RT ↓ Priority","RT ↓ Prioridad","RT ↓ Priorità","RT ↓ Priorité");
            Add("Pasta…","Folder…","Carpeta…","Cartella…","Dossier…");
            Add("Recarregar","Reload","Recargar","Ricarica","Recharger");
            Add("Nenhum mod","No mods","Sin mods","Nessuna mod","Aucun mod");
            Add("{0}/{1} • {2} ativos","{0}/{1} • {2} enabled","{0}/{1} • {2} activos","{0}/{1} • {2} attive","{0}/{1} • {2} actifs");
            Add("Aba atual","Current tab","Pestaña actual","Scheda attuale","Onglet actuel");
            Add("Alternar aba","Switch tab","Cambiar pestaña","Cambia scheda","Changer d'onglet");
            Add("A ou clique: ativar/desativar. X: configurar. Y: ativar/desativar selecionado.","A or click: enable/disable. X: configure. Y: toggle selected mod.","A o clic: activar/desactivar. X: configurar. Y: alternar el mod seleccionado.","A o clic: attiva/disattiva. X: configura. Y: alterna la mod selezionata.","A ou clic : activer/désactiver. X : configurer. Y : basculer le mod sélectionné.");
            Add(", ativado",", enabled",", activado",", attiva",", activé");
            Add(", desativado",", disabled",", desactivado",", disattivata",", désactivé");
            Add(" ativado."," enabled."," activado."," attivata."," activé.");
            Add(" desativado."," disabled."," desactivado."," disattivata."," désactivé.");
            Add(" Use Salvar alterações para aplicar."," Use Save changes to apply."," Usa Guardar cambios para aplicar."," Usa Salva modifiche per applicare."," Utilisez Enregistrer pour appliquer.");
            Add("Alterações pendentes. Use Salvar alterações para aplicar ao jogo.","Unsaved changes. Use Save changes to apply them to the game.","Cambios pendientes. Usa Guardar cambios para aplicarlos al juego.","Modifiche non salvate. Usa Salva modifiche per applicarle al gioco.","Modifications non enregistrées. Utilisez Enregistrer pour les appliquer au jeu.");
            Add("{0} • prioridade {1}. Use Salvar alterações para aplicar.","{0} • priority {1}. Use Save changes to apply.","{0} • prioridad {1}. Usa Guardar cambios para aplicar.","{0} • priorità {1}. Usa Salva modifiche per applicare.","{0} • priorité {1}. Utilisez Enregistrer pour appliquer.");
            Add("{0} mods • A/Y: ativação • X: configurar • LT/RT: subir/descer prioridade • Topo = maior prioridade","{0} mods • A/Y: enable/disable • X: configure • LT/RT: priority up/down • Top = highest priority","{0} mods • A/Y: activar/desactivar • X: configurar • LT/RT: subir/bajar prioridad • Arriba = mayor prioridad","{0} mod • A/Y: attiva/disattiva • X: configura • LT/RT: priorità su/giù • In cima = priorità massima","{0} mods • A/Y : activer/désactiver • X : configurer • LT/RT : priorité haut/bas • En haut = priorité maximale");
            Add("Não foi possível concluir: ","Could not complete: ","No se pudo completar: ","Impossibile completare: ","Impossible de terminer : ");
            Add("Entrada do controle indisponível: ","Controller input unavailable: ","Entrada del mando no disponible: ","Input del controller non disponibile: ","Entrée manette indisponible : ");
            Add("Leitura direta do controle desativada: ","Direct controller reading disabled: ","Lectura directa del mando desactivada: ","Lettura diretta del controller disattivata: ","Lecture directe de la manette désactivée : ");
            Add("Há alterações não salvas","Unsaved changes","Hay cambios sin guardar","Modifiche non salvate","Modifications non enregistrées");
            Add("Salve a ativação e a prioridade dos mods antes de continuar.","Save mod activation and priority before continuing.","Guarda la activación y prioridad de los mods antes de continuar.","Salva l'attivazione e la priorità delle mod prima di continuare.","Enregistrez l'activation et la priorité des mods avant de continuer.");
            Add("Salvar e continuar","Save and continue","Guardar y continuar","Salva e continua","Enregistrer et continuer");
            Add("Descartar","Discard","Descartar","Ignora modifiche","Ignorer");
            Add("Cancelar","Cancel","Cancelar","Annulla","Annuler");
            Add("Escolha a pasta do jogo ou a pasta mods.","Choose the game folder or the mods folder.","Elige la carpeta del juego o la carpeta mods.","Scegli la cartella del gioco o la cartella mods.","Choisissez le dossier du jeu ou le dossier mods.");
            Add("Escolha a pasta de mods.","Choose the mods folder.","Elige la carpeta de mods.","Scegli la cartella delle mod.","Choisissez le dossier des mods.");
            Add("A verificação da gravação falhou.","Save verification failed.","Falló la verificación del guardado.","Verifica del salvataggio non riuscita.","La vérification de l'enregistrement a échoué.");
            Add("Ativação e prioridade salvas. Feche e abra o jogo para aplicar.","Activation and priority saved. Restart the game to apply.","Activación y prioridad guardadas. Reinicia el juego para aplicar.","Attivazione e priorità salvate. Riavvia il gioco per applicarle.","Activation et priorité enregistrées. Relancez le jeu pour appliquer.");
            Add("O caminho é uma pasta: ","The path is a folder: ","La ruta es una carpeta: ","Il percorso è una cartella: ","Le chemin est un dossier : ");
            Add("Selecione um mod na lista.","Select a mod in the list.","Selecciona un mod de la lista.","Seleziona una mod nell'elenco.","Sélectionnez un mod dans la liste.");
            Add("Configuração fechada: ","Configuration closed: ","Configuración cerrada: ","Configurazione chiusa: ","Configuration fermée : ");
            Add("Instalar mods","Install mods","Instalar mods","Installa mod","Installer des mods");
            Add("Cole um link de instalação do HedgeModManager ou informe os IDs do GameBanana. O download será instalado na pasta selecionada.","Paste a HedgeModManager install link or enter the GameBanana IDs. The download will be installed in the selected folder.","Pega un enlace de instalación de HedgeModManager o introduce los IDs de GameBanana. La descarga se instalará en la carpeta seleccionada.","Incolla un link di installazione HedgeModManager o inserisci gli ID GameBanana. Il download verrà installato nella cartella selezionata.","Collez un lien d'installation HedgeModManager ou saisissez les identifiants GameBanana. Le téléchargement sera installé dans le dossier sélectionné.");
            Add("Link de instalação","Install link","Enlace de instalación","Link di installazione","Lien d'installation");
            Add("ID do mod (GameBanana)","Mod ID (GameBanana)","ID del mod (GameBanana)","ID mod (GameBanana)","ID du mod (GameBanana)");
            Add("ID do arquivo (mmdl)","File ID (mmdl)","ID del archivo (mmdl)","ID file (mmdl)","ID du fichier (mmdl)");
            Add("Escolha a pasta em Meus mods primeiro.","Choose the folder in My mods first.","Primero elige la carpeta en Mis mods.","Scegli prima la cartella in Le mie mod.","Choisissez d'abord le dossier dans Mes mods.");
            Add("Informe os dois IDs numéricos.","Enter both numeric IDs.","Introduce ambos IDs numéricos.","Inserisci entrambi gli ID numerici.","Saisissez les deux identifiants numériques.");
            Add("Baixando e instalando…","Downloading and installing…","Descargando e instalando…","Download e installazione…","Téléchargement et installation…");
            Add(" Novos mods precisam ser ativados e salvos."," Enable new mods and save your changes."," Activa los nuevos mods y guarda los cambios."," Attiva le nuove mod e salva le modifiche."," Activez les nouveaux mods et enregistrez les modifications.");
            Add("Explorar GameBanana","Browse GameBanana","Explorar GameBanana","Esplora GameBanana","Parcourir GameBanana");
            Add("LB / RB alternam as telas. Use o direcional e A para navegar pelos botões.","LB / RB switch tabs. Use the D-pad and A to navigate buttons.","LB / RB cambian de pestaña. Usa la cruceta y A para navegar por los botones.","LB / RB cambiano scheda. Usa la croce direzionale e A per i pulsanti.","LB / RB changent d'onglet. Utilisez la croix directionnelle et A pour les boutons.");
            Add("Voltar à instalação","Back to install","Volver a instalación","Torna all'installazione","Retour à l'installation");
            Add("Voltar página","Previous page","Página anterior","Pagina precedente","Page précédente");
            Add("Use o link de instalação na tela Instalar mods.","Use the install link on the Install mods screen.","Usa el enlace de instalación en Instalar mods.","Usa il link nella schermata Installa mod.","Utilisez le lien sur l'écran Installer des mods.");
            Add("GameBanana aberto. Copie os IDs para instalar o mod.","GameBanana opened. Copy the IDs to install the mod.","GameBanana abierto. Copia los IDs para instalar el mod.","GameBanana aperto. Copia gli ID per installare la mod.","GameBanana ouvert. Copiez les identifiants pour installer le mod.");
            Add("Não foi possível abrir o GameBanana. Você ainda pode instalar usando um link ou IDs.","Could not open GameBanana. You can still install using a link or IDs.","No se pudo abrir GameBanana. Puedes instalar mediante un enlace o IDs.","Impossibile aprire GameBanana. Puoi installare tramite un link o gli ID.","Impossible d'ouvrir GameBanana. Vous pouvez installer avec un lien ou les identifiants.");
            Add("Atualizações dos mods","Mod updates","Actualizaciones de mods","Aggiornamenti delle mod","Mises à jour des mods");
            Add("Verificar atualizações","Check for updates","Buscar actualizaciones","Cerca aggiornamenti","Rechercher les mises à jour");
            Add("Atualizar ","Update ","Actualizar ","Aggiorna ","Mettre à jour ");
            Add("Nenhum mod instalado informa um servidor de atualizações.","No installed mod provides an update server.","Ningún mod instalado proporciona un servidor de actualizaciones.","Nessuna mod installata fornisce un server di aggiornamento.","Aucun mod installé ne fournit de serveur de mise à jour.");
            Add("{0} mods verificados.","{0} mods checked.","{0} mods comprobados.","{0} mod controllate.","{0} mods vérifiés.");
            Add("Não foi possível abrir o gerenciador","Could not open the manager","No se pudo abrir el gestor","Impossibile aprire il gestore","Impossible d'ouvrir le gestionnaire");
            Add("Os arquivos dos mods não foram alterados. Detalhes da falha:","Mod files were not changed. Error details:","Los archivos de los mods no se modificaron. Detalles del error:","I file delle mod non sono stati modificati. Dettagli dell'errore:","Les fichiers des mods n'ont pas été modifiés. Détails de l'erreur :");
            Add("Tentar novamente","Try again","Reintentar","Riprova","Réessayer");
            Add("Pasta do esquema não encontrada: ","Schema folder not found: ","Carpeta del esquema no encontrada: ","Cartella dello schema non trovata: ","Dossier du schéma introuvable : ");
            Add("O mod declara um esquema que não foi encontrado: ","The mod declares a schema that was not found: ","No se encontró el esquema indicado por el mod: ","Schema dichiarato dalla mod non trovato: ","Le schéma déclaré par le mod est introuvable : ");
            Add("O esquema de configuração não contém JSON válido: ","Configuration schema contains invalid JSON: ","El esquema contiene JSON no válido: ","Lo schema contiene JSON non valido: ","Le schéma contient un JSON non valide : ");
            Add("Este mod não fornece um esquema de configuração nem um arquivo INI editável. Mods sem opções próprias não têm configurações para ajustar.","This mod provides no configuration schema or editable INI file. It has no exposed settings to adjust.","Este mod no proporciona un esquema ni un archivo INI editable. No tiene opciones disponibles para ajustar.","Questa mod non fornisce uno schema o un file INI modificabile. Non ha impostazioni disponibili da regolare.","Ce mod ne fournit ni schéma de configuration ni fichier INI modifiable. Il n'a pas d'options disponibles à régler.");
            Add("Fechar","Close","Cerrar","Chiudi","Fermer");
            Add("Arquivo de configuração","Configuration file","Archivo de configuración","File di configurazione","Fichier de configuration");
            Add("Configurar ","Configure ","Configurar ","Configura ","Configurer ");
            Add("Abrir","Open","Abrir","Apri","Ouvrir");
            Add("Salvar configurações","Save settings","Guardar configuración","Salva impostazioni","Enregistrer les réglages");
            Add("Ajustes de ","Settings for ","Ajustes de ","Impostazioni di ","Réglages de ");
            Add("Ativado","Enabled","Activado","Attivato","Activé");
            Add("Desativado","Disabled","Desactivado","Disattivato","Désactivé");
            Add("Valor atual: ","Current value: ","Valor actual: ","Valore attuale: ","Valeur actuelle : ");
            Add("Intervalo: ","Range: ","Intervalo: ","Intervallo: ","Plage : ");
            Add("sem mínimo","no minimum","sin mínimo","nessun minimo","sans minimum");
            Add("sem máximo","no maximum","sin máximo","nessun massimo","sans maximum");
            Add(" a "," to "," a "," a "," à ");
            Add("O esquema não fornece opções editáveis.","The schema provides no editable options.","El esquema no ofrece opciones editables.","Lo schema non fornisce opzioni modificabili.","Le schéma ne fournit aucune option modifiable.");
            Add("Configurar mod","Configure mod","Configurar mod","Configura mod","Configurer le mod");
            Add("O arquivo mudou desde que foi aberto. Cancele e abra a configuração novamente.","The file changed after opening. Cancel and reopen the settings.","El archivo cambió después de abrirlo. Cancela y vuelve a abrir la configuración.","Il file è cambiato dopo l'apertura. Annulla e riapri le impostazioni.","Le fichier a changé depuis son ouverture. Annulez et rouvrez les réglages.");
            Add("Esperava uma pasta: ","Expected a folder: ","Se esperaba una carpeta: ","Era prevista una cartella: ","Un dossier était attendu : ");
            Add("Editor avançado de {0}. Preserve os nomes das seções e opções fornecidas pelo autor.","Advanced editor for {0}. Preserve the section and option names provided by the author.","Editor avanzado de {0}. Conserva los nombres de secciones y opciones del autor.","Editor avanzato di {0}. Conserva i nomi delle sezioni e delle opzioni dell'autore.","Éditeur avancé de {0}. Conservez les noms des sections et options de l'auteur.");
            Add("O arquivo mudou. Cancele e abra novamente.","The file changed. Cancel and reopen it.","El archivo cambió. Cancela y vuelve a abrirlo.","Il file è cambiato. Annulla e riaprilo.","Le fichier a changé. Annulez et rouvrez-le.");
            Add("Comentário JSON não terminado.","Unterminated JSON comment.","Comentario JSON sin terminar.","Commento JSON non terminato.","Commentaire JSON non terminé.");
            Add("O arquivo deve ficar dentro da pasta do mod.","The file must be inside the mod folder.","El archivo debe estar dentro de la carpeta del mod.","Il file deve trovarsi nella cartella della mod.","Le fichier doit se trouver dans le dossier du mod.");
            Add("O arquivo está fora da pasta do mod.","The file is outside the mod folder.","El archivo está fuera de la carpeta del mod.","Il file è fuori dalla cartella della mod.","Le fichier est en dehors du dossier du mod.");
            Add("Nome ou valor INI inválido.","Invalid INI name or value.","Nombre o valor INI no válido.","Nome o valore INI non valido.","Nom ou valeur INI non valide.");
            Add("Informe um número inteiro válido.","Enter a valid integer.","Introduce un número entero válido.","Inserisci un numero intero valido.","Saisissez un entier valide.");
            Add("Informe um número válido (use ponto para decimais).","Enter a valid number (use a decimal point).","Introduce un número válido (usa punto decimal).","Inserisci un numero valido (usa il punto decimale).","Saisissez un nombre valide (utilisez un point décimal).");
            Add("Valor fora do intervalo permitido.","Value is outside the allowed range.","Valor fuera del intervalo permitido.","Valore fuori dall'intervallo consentito.","Valeur hors de la plage autorisée.");
            Add("Sem servidor de atualizações","No update server","Sin servidor de actualizaciones","Nessun server di aggiornamento","Aucun serveur de mise à jour");
            Add("Falha na verificação: ","Check failed: ","Error de comprobación: ","Verifica non riuscita: ","Échec de la vérification : ");
            Add("Versão remota não informada","No VersionString","Versión remota no indicada","Versione remota non specificata","Version distante non indiquée");
            Add("Atualização disponível","Update available","Actualización disponible","Aggiornamento disponibile","Mise à jour disponible");
            Add("Atualizado","Up to date","Actualizado","Aggiornato","À jour");
            Add("Nenhuma atualização para aplicar.","No update to apply.","Ninguna actualización para aplicar.","Nessun aggiornamento da applicare.","Aucune mise à jour à appliquer.");
            Add("Atualização aplicada.","Update applied.","Actualización aplicada.","Aggiornamento applicato.","Mise à jour appliquée.");
            Add("Falha ao baixar mod_files.txt: ","Failed to fetch mod_files.txt: ","Error al descargar mod_files.txt: ","Download di mod_files.txt non riuscito: ","Échec du téléchargement de mod_files.txt : ");
            Add("Solicitação de instalação inválida.","Invalid install request.","Solicitud de instalación no válida.","Richiesta di installazione non valida.","Demande d'installation non valide.");
            Add("Falha no download: ","Download failed: ","Error de descarga: ","Download non riuscito: ","Échec du téléchargement : ");
            Add("Falha ao extrair o arquivo: ","Unsupported archive (extract failed): ","Error al extraer el archivo: ","Estrazione dell'archivio non riuscita: ","Échec de l'extraction de l'archive : ");
            Add("mod.ini não encontrado no arquivo.","No mod.ini found in archive.","No se encontró mod.ini en el archivo.","mod.ini non trovato nell'archivio.","mod.ini introuvable dans l'archive.");
            Add("Instalado em ","Installed to ","Instalado en ","Installato in ","Installé dans ");
            Add("O link está vazio.","Link is empty.","El enlace está vacío.","Il link è vuoto.","Le lien est vide.");
            Add("Link de instalação inválido.","Invalid install URL.","Enlace de instalación no válido.","Link di installazione non valido.","Lien d'installation non valide.");
        }
    }
}
