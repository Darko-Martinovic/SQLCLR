using System;
// ReSharper disable once CheckNamespace
namespace SqlClrCustomSendMail
{
    internal static class DetermineStyle
    {
        private const string StRed = "ST_RED";
        private const string StGreen = "ST_GREEN";
        private const string StRose = "ST_ROSE";
        private const string StBrown = "ST_BROWN";
        private const string StBlack = "ST_BLACK";
        public const string StBlue = "ST_BLUE";
        private const string StSimple = "ST_SIMPLE";
        private const string StNoStyle = "ST_NOSTYLE";


        public static string DetermineStyleName(string style)
        {
            var styleName = "unknowen";
            string[] tester;

            if (style.Contains("table."))
            {
                tester = style.Split(new[] { "table." }, StringSplitOptions.RemoveEmptyEntries);
                var valueString = tester[1];
                styleName = valueString.Substring(0, valueString.IndexOf("{", StringComparison.Ordinal)).Trim();
                // ReSharper disable once RedundantAssignment
                valueString = null;
            }

            // ReSharper disable once RedundantAssignment
            tester = null;
            return styleName;

        }



        public static string GetStyle(string style)
        {
            var defaultStyle = @"
    <style type='text/css'>
        .datagridST_BLUE table {
            text-align: left;
            width: 100%;
           
        }

        .datagridST_BLUE {
            font: normal 12px/150% Arial, Helvetica, sans-serif;
            background: #fff;
            overflow: hidden;
           
        }

            .datagridST_BLUE table td, .datagrid table th {
                padding: 1px 1px;
            }

            .datagridST_BLUE table caption {
                padding: 1px 1px;
                font-size: 12px;
                background: #E1EEf4;
                color: #00557F;
                font-weight: bold;
            }

            .datagridST_BLUE table thead th {
                background: -webkit-gradient( linear, left top, left bottom, color-stop(0.05, #006699), color-stop(1, #00557F) );
                background: -moz-linear-gradient( center top, #006699 5%, #00557F 100% );
                filter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#006699', endColorstr='#00557F');
                background-color: #006699;
                color: #FFFFFF;
                font-size: 12px;
                font-weight: bold;
                border: 0.2px dotted #0070A8;
            }

              

            .datagridST_BLUE table tbody td {
                color: #00557F;
                border: 0.2px dotted #E1EEF4;
                font-size: 12px;
                font-weight: normal;
            }

            .datagridST_BLUE table tbody .alt td {
                background: #E1EEf4;
                color: #00557F;
            }

           

           
            .datagridST_BLUE table tfoot td div {
                border: 0.2px dotted #006699;
                background: #E1EEf4;
            }

            .datagridST_BLUE table tfoot td {
                padding: 0;
                color: #00557F;
                font-size: 11px;
            }

            .datagridST_BLUE tr:hover {
                 background: yellow;
            }

            .datagridST_BLUE td:hover {
                 background: yellow;
            }

              



        div.dhtmlx_window_active, div.dhx_modal_cover_dv {
            position: fixed !important;
        }
    </style>

";

            switch (style)
            {
                //Apply default style 
                case StRed:
                    defaultStyle = @"
    <style type='text/css'>
        .datagridST_RED table {
           text-align: left;
            width: 100%;
        }

        .datagridST_RED {
            font: normal 12px/150% Arial, Helvetica, sans-serif;
            background: #fff;
            overflow: hidden;
        }

          .datagridST_RED table td, .datagrid table th {
                padding: 1px 1px;
            }



            .datagridST_RED table caption {
                padding: 1px 1px;
                font-size: 12px;
                background: #F7CDCD;
                color: #80141C;
                font-weight: bold;
            }

            .datagridST_RED table thead th {
                background: -webkit-gradient( linear, left top, left bottom, color-stop(0.05, #991821), color-stop(1, #80141C) );
                background: -moz-linear-gradient( center top, #991821 5%, #80141C 100% );
                filter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#991821', endColorstr='#80141C');
                background-color: #991821;
                color: #FFFFFF;
                font-size: 12px;
                font-weight: bold;
                border: 0.2px dotted #B01C26;
            }

          

            .datagridST_RED table tbody td {
                color: #80141C;
                border: 0.2px dotted #F7CDCD;
                font-size: 12px;
                font-weight: normal;
            }

            .datagridST_RED table tbody .alt td {
                background: #F7CDCD;
                color: #80141C;
            }

           

            .datagridST_RED table tfoot td div {
                border: 0.2px dotted #991821;
                background: #F7CDCD;
            }

            .datagridST_RED table tfoot td {
                padding: 0;
                color: #80141C;
                font-size: 11px;
            }

            .datagridST_RED tr:hover {
                 background: yellow;
            }
            .datagridST_RED td:hover {
                 background: yellow;
            }


              
        div.dhtmlx_window_active, div.dhx_modal_cover_dv {
            position: fixed !important;
        }
    </style>";
                    break;
                case StGreen:
                    defaultStyle = @"
    <style type='text/css'>
        .datagridST_GREEN table {
            text-align: left;
            width: 100%;
        }

        .datagridST_GREEN {
            font: normal 12px/150% Arial, Helvetica, sans-serif;
            background: #fff;
            overflow: hidden;
        }

           .datagridST_GREEN table td, .datagrid table th {
                padding: 1px 1px;
            }
            .datagridST_GREEN table caption {
                padding: 1px 1px;
                font-size: 12px;
                background: #DFFFDE;
                color: #275420;
                font-weight: bold;
            }


            .datagridST_GREEN table thead th {
                background: -webkit-gradient( linear, left top, left bottom, color-stop(0.05, #36752D), color-stop(1, #275420) );
                background: -moz-linear-gradient( center top, #36752D 5%, #275420 100% );
                filter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#36752D', endColorstr='#275420');
                background-color: #36752D;
                color: #FFFFFF;
                font-size: 12px;
                font-weight: bold;
                border: 0.2px dotted #36752D;
            }

            

            .datagridST_GREEN table tbody td {
                color: #275420;
                border: 0.2px dotted #C6FFC2;
                font-size: 12px;
                font-weight: normal;
            }

            .datagridST_GREEN table tbody .alt td {
                background: #DFFFDE;
                color: #275420;
            }

             .datagridST_GREEN table tfoot td div {
                border: 0.2px dotted #36752D;
                background: #F7CDCD;
            }

            .datagridST_GREEN table tfoot td {
                padding: 0;
                color: #275420;
                font-size: 11px;
            }

            .datagridST_GREEN tr:hover {
                 background: yellow;
            }
            .datagridST_GREEN td:hover {
                 background: yellow;
            }

           
        div.dhtmlx_window_active, div.dhx_modal_cover_dv {
            position: fixed !important;
        }
    </style>";
                    break;
                case StRose:
                    defaultStyle = @"
    <style type='text/css'>
        .datagridST_ROSE table {
            text-align: left;
            width: 100%;
        }

        .datagridST_ROSE {
            font: normal 12px/150% Arial, Helvetica, sans-serif;
            background: #fff;
            overflow: hidden;
        }

           .datagridST_ROSE table td, .datagrid table th {
                padding: 1px 1px;
            }

            .datagridST_ROSE table caption {
                padding: 1px 1px;
                font-size: 12px;
                background: #F4E3FF;
                color: #4D1A75;
                font-weight: bold;
            }


            .datagridST_ROSE table thead th {
                background: -webkit-gradient( linear, left top, left bottom, color-stop(0.05, #652299), color-stop(1, #4D1A75) );
                background: -moz-linear-gradient( center top, #652299 5%, #4D1A75 100% );
                filter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#652299', endColorstr='#4D1A75');
                background-color: #652299;
                color: #FFFFFF;
                font-size: 12px;
                font-weight: bold;
                border: 0.2px dotted #714399;
            }

             

            .datagridST_ROSE table tbody td {
                color: #4D1A75;
                border: 0.2px dotted #E7BDFF;
                font-size: 12px;
                font-weight: normal;
            }

            .datagridST_ROSE table tbody .alt td {
                background: #F4E3FF;
                color: #4D1A75;
            }

           

            .datagridST_ROSE table tfoot td div {
                border: 0.2px dotted #652299;
                background: #F4E3FF;
            }

            .datagridST_ROSE table tfoot td {
                padding: 0;
                font-size: 11px;
                color: #4D1A75;
            }

               
            .datagridST_ROSE tr:hover {
                 background: yellow;
            }
            .datagridST_ROSE td:hover {
                 background: yellow;
            }


        div.dhtmlx_window_active, div.dhx_modal_cover_dv {
            position: fixed !important;
        }
    </style>";
                    break;
                case StBlack:
                    defaultStyle = @"
    <style type='text/css'>
        .datagridST_BLACK table {
            text-align: left;
            width: 100%;
        }

        .datagridST_BLACK {
            font: normal 12px/150% Arial, Helvetica, sans-serif;
            background: #fff;
            overflow: hidden;
        }

             .datagridST_BLACK table td, .datagrid table th {
                padding: 1px 1px;
            }

            .datagridST_BLACK table caption {
                padding: 1px 1px;
                font-size: 12px;
                background: #EBEBEB;
                color: #7D7D7D;
                font-weight: bold;
            }

            .datagridST_BLACK table thead th {
                background: -webkit-gradient( linear, left top, left bottom, color-stop(0.05, #8C8C8C), color-stop(1, #7D7D7D) );
                background: -moz-linear-gradient( center top, #8C8C8C 5%, #7D7D7D 100% );
                filter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#8C8C8C', endColorstr='#7D7D7D');
                background-color: #8C8C8C;
                color: #FFFFFF;
                font-size: 12px;
                font-weight: bold;
                border: 0.2px dotted #A3A3A3;
            }

              
            .datagridST_BLACK table tbody td {
                color: #7D7D7D;
                border: 0.2px dotted #DBDBDB;
                font-size: 12px;
                font-weight: normal;
            }

            .datagridST_BLACK table tbody .alt td {
                background: #EBEBEB;
                color: #7D7D7D;
            }

          
            .datagridST_BLACK table tfoot td div {
                border: 0.2px dotted #DBDBDB;
                background: #EBEBEB;
            }

            .datagridST_BLACK table tfoot td {
                padding: 0;
                font-size: 11px;
                color: #7D7D7D;
            }


            .datagridST_BLACK tr:hover {
                 background: yellow;
            }
            .datagridST_BLACK td:hover {
                 background: yellow;
            }

            


        div.dhtmlx_window_active, div.dhx_modal_cover_dv {
            position: fixed !important;
        }
    </style>";
                    break;
                case StBrown:
                    defaultStyle = @"
    <style type='text/css'>
        .datagridST_BROWN table {
            text-align: left;
            width: 100%;
        }

        .datagridST_BROWN {
            font: normal 12px/150% Arial, Helvetica, sans-serif;
            background: #fff;
            overflow: hidden;
        }

           .datagridST_BROWN table td, .datagrid table th {
                padding: 1px 1px;
            }

            .datagridST_BROWN table caption {
                padding: 1px 1px;
                font-size: 12px;
                background: #F0E5CC;
                color: #7F4614;
                font-weight: bold;
            }


            .datagridST_BROWN table thead th {
                background: -webkit-gradient( linear, left top, left bottom, color-stop(0.05, #A65B1A), color-stop(1, #7F4614) );
                background: -moz-linear-gradient( center top, #A65B1A 5%, #7F4614 100% );
                filter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#A65B1A', endColorstr='#7F4614');
                background-color: #A65B1A;
                color: #FFFFFF;
                font-size: 12px;
                font-weight: bold;
                border: 0.2px dotted #BF691E;
            }

              
            .datagridST_BROWN table tbody td {
                color: #7F4614;
                border: 0.2px dotted #D9CFB8;
                font-size: 12px;
                font-weight: normal;
            }

            .datagridST_BROWN table tbody .alt td {
                background: #F0E5CC;
                color: #7F4614;
            }

           
            .datagridST_BROWN table tfoot td div {
                border: 0.2px dotted #A65B1A;
                background: #F0E5CC;
            }

            .datagridST_BROWN table tfoot td {
                padding: 0;
                color: #7F4614;
                font-size: 11px;
            }

            .datagridST_BROWN tr:hover {
                 background: yellow;
            }
            .datagridST_BROWN td:hover {
                 background: yellow;
            }

        div.dhtmlx_window_active, div.dhx_modal_cover_dv {
            position: fixed !important;
        }
    </style>";
                    break;
                case StSimple:
                    defaultStyle = @"
                    <style type='text/css'> 
                    .datagridST_SIMPLE table {
                    { 
                        font-family: verdana,arial,sans-serif;
                        font-size:11px;
                        color:#333333; 
                        border-width: 0.2px;
                        border-color: #666666;
                        border-collapse: collapse;
                    } 
                    .datagridST_SIMPLE th 
                    { 
                        border-width: 0.2px;
                        padding: 6px;
                        border-style: dotted;
                        border-color: #666666;
                        background-color: #dedede;
                    } 
                    .datagridST_SIMPLE td 
                    { 
                        border-width: 0.2px;
                        padding: 6px;
                        border-style: dotted;
                        border-color: #666666;
                        background-color: #ffffff;
                    } 
                    .datagridST_SIMPLE thead 
                    { 
                        font-weight: bold;
                        font-family: verdana,arial,sans-serif;
                        font-size:13px;
                        text-align: center;
                        background-color: #dedede;
                    } 
                    .datagridST_SIMPLE tfoot 
                    { 
                        font-family: verdana,arial,sans-serif;
                        font-size:11px;
                        text-align: center;
                    } 
                    </style>
                    ";
                    break;
                case StNoStyle:
                    defaultStyle = @"
    <style type='text/css'>
        .datagridST_NO_STYLE table {
        }
    </style>";
                    break;
                default:
                    if (style != "" && style != StBlue)
                    {
                        defaultStyle = style;
                    }

                    break;
            }
            return defaultStyle;

        }

    }
}

