﻿using System;
using System.Data;
using System.Collections;
using System.Windows.Forms;
using System.Configuration;
using ActionsList.DAL;
using ActionsList.BO;
using ActionsList.BLL;

namespace ActionsList.GUI
{
    public partial class Toolbox : Form
    {
        public Toolbox()
        {
            InitializeComponent();
        }

        // Chargement des différents composants.
        private void Toolbox_Load(object sender, EventArgs e)
        {
            this.Text += " v"+Application.ProductVersion;

            filterCombo.Items.AddRange(ReadDB.Instance.getFiltersName()); 
            
            foreach (object item in ReadDB.Instance.getCtxt())
                ctxtListBox.Items.Add(item,true); // Remplissage de la combobox contextes en sélectionnant par défaut

            foreach (object item in ReadDB.Instance.getDest()) // C'est pas bien mais il n'y a pas de code business
                destListBox.Items.Add(item,true); // Remplissage de la combobox destinataires en sélectionnant par défaut

            foreach (object item in ReadDB.Instance.getStatut()) // C'est pas bien mais il n'y a pas de code business
                statutListBox.Items.Add(item,true); // Remplissage de la combobox statuts en sélectionnant par défaut

        }
        
        // Rafraîchissement de la page
        public void miseAjour(object sender, EventArgs e)
        {           
            // Mise à jour de la liste avec le dernier filtre joué
            if(Filtre.CurrentFilter != null)
                afficheActions(Filtre.CurrentFilter);

            /*
            //Mise à jour de la liste des contextes
            if(bilan.Contains("Nouveau contexte"))
                foreach (String item in services.getCtxt())
                    if (!ctxtListBox.Items.Contains(item))
                        ctxtListBox.Items.Add(item);

            // Mise à jour de la liste des sujets
            if(ctxtListBox.CheckedItems.Count==1 && bilan.Contains("Nouveau sujet"))
                foreach (String item in services.getSujet((String)ctxtListBox.CheckedItems[0]))
                    if (!sujetListBox.Items.Contains(item))
                        sujetListBox.Items.Add(item);

            //Mise à jour de la liste des destinataires
            if (bilan.Contains("Nouveau destinataire"))
                foreach (String item in services.getDest())
                    if (!destListBox.Items.Contains(item))
                        destListBox.Items.Add(item);
             */
        }

        // Ouverture de la gui création d'action
        private void ajoutAction(object sender, EventArgs e)
        {
            ManipAction fenetre = new ManipAction();
            fenetre.FormClosed += new FormClosedEventHandler(this.miseAjour); // Sur fermeture de ManipAction on update la Toolbox
            fenetre.Show();
        }

        // Ouverture de la gui édition d'action
        private void modifAction(object sender, DataGridViewCellEventArgs e)
        {
            // Ouverture de la fenêtre ManipAction en mode édition
            ManipAction fenetre = new ManipAction(grilleData.Rows[e.RowIndex].Cells);
            fenetre.FormClosed += new FormClosedEventHandler(this.miseAjour); // Sur fermeture de ManipAction on update la Toolbox
            fenetre.Show();
        }

        // Méthode appelée quand checks des contextes changent
        private void updateSujet(object sender, EventArgs e)
        {     
            // Dans tous les cas de changements de séléction on vide la liste
            sujetListBox.Items.Clear();

            // On n'affiche la liste des sujets que si un seul contexte est sélectionné
            if (ctxtListBox.CheckedItems.Count == 1)
            {
                //Activation des radio buttons
                SujSelRadio.Enabled = true;
                SujAllRadio.Enabled = true;
                //Remplissage de la liste
                foreach (object item in ReadDB.Instance.getSujet((String)ctxtListBox.CheckedItems[0].ToString()))
                    sujetListBox.Items.Add(item, true);
            }
            else
            {
                //Dans tous les autres cas on grise les radio buttons
                SujSelRadio.Enabled = false;
                SujAllRadio.Enabled = false;
                //Et on active "Sélection"
                //SujSelRadio.Checked = true;
            }

        }

        // Méthode générique d'affichage de la liste d'actions à partir d'un filtre
        private void afficheActions(Filtre filtre)
        {
            DataTable liste = DataManager.Instance.getActions(filtre);

            if (liste.Rows.Count > 0)
                grilleData.DataSource = liste.DefaultView;
            else
            {
                MessageBox.Show("Aucun résultat ne correspond au filtre", "Filtre d'actions", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //grilleData.Rows.Clear(); //TODO:Il faut effacer la grille quand il n'y a pas de résultat
            }
        }

        // Affichage des actions sur filtre manuel
        private void filtreAction(object sender, EventArgs e)
        {
            Filtre filtre = new Filtre(CtxtAllRadio.Checked,ctxtListBox.CheckedItems,SujAllRadio.Checked,sujetListBox.CheckedItems,destAllRadio.Checked,destListBox.CheckedItems,statAllRadio.Checked,statutListBox.CheckedItems);
            this.afficheActions(filtre);
        }

        // Fermeture de la Form si minimisée
        private void Toolbox_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.Close();
        }

        // Affichage du menu contextuel sur le tableau d'action
        private void grilleData_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                //Sélection de la ligne
                grilleData.Rows[e.RowIndex].Selected = true;
                //Affichage du menu contextuel
                listeContext.Show(Cursor.Position);
            }
        }

        // Copie de l'action dans le presse papier avec le format SEnS => couche business
        private void copySens(object sender, EventArgs e)
        {
            Export.Instance.clipAction(ConfigurationManager.AppSettings["sensTemplate"], grilleData.SelectedRows[0]);
        }

        // Copie de l'action dans le presse papier avec le format PRM SBA => couche business
        private void copySBA(object sender, EventArgs e)
        {
            Export.Instance.clipAction(ConfigurationManager.AppSettings["sbaTemplate"], grilleData.SelectedRows[0]);
        }
        
        // Enregistrement du filtre renseigné
        private void saveFilterBut_Click(object sender, EventArgs e)
        {
            if (filterCombo.Text != "")
            {
                Filtre filtre = new Filtre(CtxtAllRadio.Checked,ctxtListBox.CheckedItems,SujAllRadio.Checked,sujetListBox.CheckedItems,destAllRadio.Checked,destListBox.CheckedItems,statAllRadio.Checked,statutListBox.CheckedItems);
                if(ReadDB.Instance.isNvoFiltre(filterCombo.Text))
                {
                    WriteDB.Instance.insertFiltre(filterCombo.Text, filtre.criteria);
                    // On vide la liste des filtres et on efface la sélection
                    filterCombo.Items.Clear();
                    filterCombo.Text = "";
                    // On la remplit à nouveau
                    filterCombo.Items.AddRange(ReadDB.Instance.getFiltersName()); 
                }                  
                else
                    MessageBox.Show("Ce nom de filtre existe déjà.", "Nom du filtre", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);               
            }
            else
                MessageBox.Show("Le nom du filtre ne peut être vide", "Sauvegarde de filtre", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);     
        }

        //Routine générique permettant de griser les listes et sélectionner tous les items
        private void DisableAndSelectAll(CheckedListBox list, RadioButton radio)
        {
            for (int i = 0; i < list.Items.Count; i++)
                list.SetItemChecked(i, true);

            list.Enabled = !radio.Checked;
        }

        //Application sur liste contexte
        private void CtxtAllRadio_CheckedChanged(object sender, EventArgs e)
        {
            DisableAndSelectAll(ctxtListBox, (RadioButton)sender);
        }

        //Application sur liste destinataires
        private void destAllRadio_CheckedChanged(object sender, EventArgs e)
        {
            DisableAndSelectAll(destListBox, (RadioButton)sender);
        }

        //Application sur liste sujets
        private void SujAllRadio_CheckedChanged(object sender, EventArgs e)
        {
            DisableAndSelectAll(sujetListBox, (RadioButton)sender);
        }

        //Application sur liste statuts
        private void statAllRadio_CheckedChanged(object sender, EventArgs e)
        {
            DisableAndSelectAll(statutListBox, (RadioButton)sender);
        }

        // Méthode générique utilisée pour appliquer un filtre dans les CheckedListBox
        private void checkSelection(RadioButton selRadio,CheckedListBox liste, IList criteres)
        {
            selRadio.Checked = true; // Sélection du radio Sélection
            for (int i = 0; i < liste.Items.Count; i++) // Parcours de la ListBox
            {
                int index = criteres.IndexOf(liste.Items[i]); // Recherche de l'item dans le filtre
                liste.SetItemChecked(i, !(index == -1));
            }    
        }

        // Application d'un filtre enregistré
        private void openFilterBut_Click(object sender, EventArgs e)
        {
            if (filterCombo.Text != "")
            {
                // Récupération du filtre correspondant
                Filtre filtre = ReadDB.Instance.getFilter(filterCombo.Text);
                
                // Passage de toutes les checkboxes à ALL
                CtxtAllRadio.Checked = true;
                destAllRadio.Checked = true;
                statAllRadio.Checked = true;

                // Tickage des bons critères
                foreach (Criterium critere in filtre.criteria)
                {
                    switch (critere.champ)
                    {
                        case(0): //Contexte
                            checkSelection(CtxtSelRadio, ctxtListBox, critere.selected);                          
                            break;
                        case(1): //Sujet
                            checkSelection(SujSelRadio, sujetListBox, critere.selected);                          
                            break;
                        case(2): //Destinataire
                            checkSelection(destSelRadio, destListBox, critere.selected);                          
                            break;
                        case(3): //Statut
                            checkSelection(statSelRadio, statutListBox, critere.selected);
                            break;
                    }
                }

                // Application du filtre
                afficheActions(filtre);
            }
            else
                MessageBox.Show("Veuillez entrer un nom de filtre", "Application d'un filtre", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

    }
}